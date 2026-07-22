using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc;
using dc.libs.misc;
using HaxeProxy.Runtime;

namespace DeadCellsMultiplayerX.Common
{
    public class KinematicPositionSmoother
    {
        // ---------- 配置 ----------
        private const double TileSize = 24.0;
        private readonly Tweenie tweenie;
        private readonly Entity entity;

        /// <summary>补间时长（毫秒）</summary>
        public double SmoothDurationMs { get; set; } = 20;

        /// <summary>超过此像素距离将被视为传送，直接设置位置</summary>
        public double TeleportThresholdPx { get; set; } = 300.0;


        private Tween? tween;
        private double fromX, fromY;
        private double toX, toY;
        private double currentX, currentY;
        private double progress;


        public KinematicPositionSmoother(Entity entity, Tweenie tweenie)
        {
            this.entity = entity;
            this.tweenie = tweenie;
        }


        /// <summary>
        /// 初次或传送后调用
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        public void SetInitialPosition(double pixelX, double pixelY)
        {
            fromX = toX = currentX = pixelX;
            fromY = toY = currentY = pixelY;
            CommitPosition(pixelX, pixelY);
            EnsureTween();
        }

        /// <summary>
        /// 设置新的目标像素位置。内部自动处理传送检测与补间重启。
        /// </summary>
        /// <param name="targetX"></param>
        /// <param name="targetY"></param>
        public void SetTargetPosition(double targetX, double targetY)
        {
            if (tween == null)
            {
                SetInitialPosition(targetX, targetY);
                return;
            }

            double dx = targetX - currentX;
            double dy = targetY - currentY;
            if (dx * dx + dy * dy > TeleportThresholdPx * TeleportThresholdPx)
            {
                SetInitialPosition(targetX, targetY);
                return;
            }

            fromX = currentX;
            fromY = currentY;
            toX = targetX;
            toY = targetY;
            EnsureTween();
        }


        private void CommitPosition(double pixelX, double pixelY)
        {
            int cx = (int)(pixelX / TileSize);
            int cy = (int)(pixelY / TileSize);
            double xr = (pixelX - cx * TileSize) / TileSize;
            double yr = (pixelY - cy * TileSize) / TileSize;
            entity.setPosCase(cx, cy, xr, yr);
        }

        private void EnsureTween()
        {
            double speed = 1.0 / (SmoothDurationMs * tweenie.baseFps / 1000.0);

            if (tween != null && !tween.done)
            {
                tween.from = 0.0;
                tween.to = 1.0;
                tween.ln = 0.0;
                tween.speed = speed;
            }
            else
            {
                tween = tweenie.create_(
                    getter: () => progress,
                    setter: (val) =>
                    {
                        progress = val;
                        currentX = fromX + val * (toX - fromX);
                        currentY = fromY + val * (toY - fromY);
                        CommitPosition(currentX, currentY);
                    },
                    from: 0.0,
                    to: 1.0,
                    tp: new TType.TLinear(),
                    duration_ms: SmoothDurationMs,
                    allowDuplicates: Ref<bool>.In(true)
                );
            }
        }
    }
}