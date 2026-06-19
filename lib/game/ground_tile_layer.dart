import 'dart:math' as math;
import 'package:flame/components.dart';
import 'package:flutter/material.dart';

class GroundTileLayer extends PositionComponent {
  GroundTileLayer({required Vector2 size}) : super(size: size, priority: -10);
  static const double tileW = 96, tileH = 48;

  @override
  void render(Canvas canvas) {
    final cols = (size.x / tileW).ceil() + 2;
    final rows = (size.y / tileH).ceil() + 4;
    final rng = math.Random(42);
    for (var r = -2; r < rows; r++) {
      for (var c = -1; c < cols; c++) {
        final offsetX = (r.isOdd) ? tileW / 2 : 0;
        final cx = c * tileW + offsetX;
        final cy = r * tileH;
        final shade = 0.55 + rng.nextDouble() * 0.18;
        final color = Color.fromRGBO(
          (107 * shade).round().clamp(0, 255),
          (99 * shade).round().clamp(0, 255),
          (88 * shade).round().clamp(0, 255), 1.0);
        final path = Path()
          ..moveTo(cx + tileW / 2, cy)
          ..lineTo(cx + tileW, cy + tileH / 2)
          ..lineTo(cx + tileW / 2, cy + tileH)
          ..lineTo(cx, cy + tileH / 2)
          ..close();
        canvas.drawPath(path, Paint()..color = color);
        canvas.drawPath(path, Paint()..color = const Color(0x33000000)..style = PaintingStyle.stroke..strokeWidth = 1);
      }
    }
    // Decorative greco columns (broken marble)
    final colRng = math.Random(11);
    for (var i = 0; i < 8; i++) {
      final x = 200 + colRng.nextDouble() * (size.x - 400);
      final y = 200 + colRng.nextDouble() * (size.y - 400);
      _drawBrokenColumn(canvas, Offset(x, y));
    }
  }

  void _drawBrokenColumn(Canvas canvas, Offset center) {
    canvas.drawOval(Rect.fromCenter(center: center.translate(0, 12), width: 60, height: 18),
        Paint()..color = const Color(0x55000000));
    canvas.drawRect(Rect.fromCenter(center: center, width: 36, height: 56),
        Paint()..color = const Color(0xFFCFC2A8));
    canvas.drawRect(Rect.fromCenter(center: center.translate(0, -32), width: 48, height: 12),
        Paint()..color = const Color(0xFFE8DCBE));
  }
}
