using System;
using System.Threading;
using AutoWizard.Core.Input;

namespace AutoWizard.Core.Actions.Input
{
    /// <summary>真人模擬的滑鼠移動（二次貝茲曲線）。實際輸入透過傳入的 <see cref="IInputSimulator"/> 送出。</summary>
    public static class MouseMovementHelper
    {
        private static Random _random = new Random();

        public static void MoveMouseSmoothly(IInputSimulator input, int startX, int startY, int endX, int endY, int durationMs = 500)
        {
            // 如果距離很短，直接移動
            double distance = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
            if (distance < 10)
            {
                input.MoveMouse(endX, endY);
                return;
            }

            // 產生貝茲曲線控制點
            // 控制點在起點和終點之間，並加入隨機偏移
            int controlX = (startX + endX) / 2 + _random.Next(-100, 100);
            int controlY = (startY + endY) / 2 + _random.Next(-100, 100);

            // 步數取決於持續時間 (每 10ms 一步)
            int steps = Math.Max(10, durationMs / 10);

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;

                // 二次貝茲曲線公式
                // B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                double u = 1 - t;
                double tt = t * t;
                double uu = u * u;

                int x = (int)(uu * startX + 2 * u * t * controlX + tt * endX);
                int y = (int)(uu * startY + 2 * u * t * controlY + tt * endY);

                input.MoveMouse(x, y);
                Thread.Sleep(10); // 控制速度
            }

            // 確保最後位置準確
            input.MoveMouse(endX, endY);
        }
    }
}
