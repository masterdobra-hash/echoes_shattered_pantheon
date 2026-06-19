import 'package:flame/components.dart';
import 'package:flutter/material.dart';

class LootDropComponent extends PositionComponent {
  LootDropComponent({required Vector2 position, required this.rarityColor, required this.label})
      : super(position: position, anchor: Anchor.center, priority: 6);
  final Color rarityColor;
  final String label;
  double _bob = 0;

  @override
  void update(double dt) {
    super.update(dt);
    _bob += dt * 2;
  }

  @override
  void render(Canvas canvas) {
    final yOff = (3 * (0.5 + 0.5 * (1 - (1 - (_bob % 2 - 1).abs())))).toDouble();
    canvas.drawRect(
      Rect.fromCenter(center: Offset(0, -yOff), width: 14, height: 90),
      Paint()..color = rarityColor.withOpacity(0.40)..maskFilter = const MaskFilter.blur(BlurStyle.normal, 10),
    );
    canvas.drawCircle(Offset(0, -yOff), 10, Paint()..color = rarityColor);
    canvas.drawCircle(Offset(0, -yOff), 10,
        Paint()..color = Colors.white..style = PaintingStyle.stroke..strokeWidth = 1.5);
    final tp = TextPainter(
      text: TextSpan(
        text: label,
        style: TextStyle(
          color: rarityColor, fontSize: 11, fontWeight: FontWeight.bold,
          shadows: const [Shadow(offset: Offset(1, 1), blurRadius: 2, color: Colors.black)],
        ),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    tp.paint(canvas, Offset(-tp.width / 2, -yOff - 28));
  }
}
