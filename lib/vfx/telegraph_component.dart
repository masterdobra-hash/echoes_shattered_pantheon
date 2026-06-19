import 'package:flame/components.dart';
import 'package:flutter/material.dart';

class TelegraphComponent extends PositionComponent {
  TelegraphComponent({required Vector2 position, required this.radius, this.lifetime = 0.9})
      : super(position: position, anchor: Anchor.center, priority: 3);
  final double radius;
  double lifetime;
  double _life = 0;

  @override
  Future<void> onLoad() async {
    _life = lifetime;
  }

  @override
  void update(double dt) {
    super.update(dt);
    _life -= dt;
    if (_life <= 0) removeFromParent();
  }

  @override
  void render(Canvas canvas) {
    final t = (1.0 - (_life / lifetime)).clamp(0.0, 1.0);
    canvas.drawCircle(Offset.zero, radius,
        Paint()..color = const Color(0xFFFF3030).withOpacity(0.35 * (0.5 + 0.5 * t)));
    canvas.drawCircle(Offset.zero, radius,
        Paint()..color = const Color(0xFFFF3030)..style = PaintingStyle.stroke..strokeWidth = 3);
  }
}

class HitFlashComponent extends PositionComponent {
  HitFlashComponent({required Vector2 position, required this.color, required this.radius})
      : super(position: position, anchor: Anchor.center, priority: 7);
  final Color color;
  final double radius;
  double _life = 0.25;

  @override
  void update(double dt) {
    super.update(dt);
    _life -= dt;
    if (_life <= 0) removeFromParent();
  }

  @override
  void render(Canvas canvas) {
    final alpha = (_life / 0.25).clamp(0.0, 1.0);
    canvas.drawCircle(Offset.zero, radius,
        Paint()..color = color.withOpacity(0.5 * alpha)..maskFilter = const MaskFilter.blur(BlurStyle.normal, 12));
    canvas.drawCircle(Offset.zero, radius * 0.6,
        Paint()..color = Colors.white.withOpacity(0.7 * alpha));
  }
}

class FloatingDamageText extends PositionComponent {
  FloatingDamageText({required Vector2 position, required this.text, required this.color})
      : super(position: position, priority: 10);
  final String text;
  final Color color;
  double _life = 0.9;

  @override
  void update(double dt) {
    super.update(dt);
    _life -= dt;
    position.y -= dt * 30;
    if (_life <= 0) removeFromParent();
  }

  @override
  void render(Canvas canvas) {
    final alpha = (_life / 0.9).clamp(0.0, 1.0);
    final tp = TextPainter(
      text: TextSpan(
        text: text,
        style: TextStyle(
          color: color.withOpacity(alpha),
          fontSize: 18,
          fontWeight: FontWeight.bold,
          shadows: const [Shadow(offset: Offset(1, 1), blurRadius: 3, color: Colors.black)],
        ),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    tp.paint(canvas, Offset(-tp.width / 2, 0));
  }
}
