using CmnDxlib;
using System;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// Viewのベースクラス
    /// </summary>
    public partial class BaseView : IDisposable
    {
        public BaseView()
        {

        }

        public virtual void Dispose()
        {

        }

        public virtual void Draw()
        {

        }

        /// <summary>
        /// カメラの位置・向きを更新する
        /// </summary>
        /// <param name="targetPosition">
        /// カメラが注視するターゲット（例：プレイヤー座標）
        /// </param>
        /// <param name="cameraDistance">
        /// ターゲットから見たカメラの相対位置
        /// （回転前のオフセットベクトル）
        /// </param>
        /// <param name="cameraAngleHorizon">
        /// 水平方向の回転角度（度）
        /// Y軸回転に使用
        /// </param>
        /// <param name="cameraAngleVertical">
        /// 垂直方向の回転角度（度）
        /// X軸回転に使用
        /// </param>
        /// <param name="CollModelNo">
        /// カメラとターゲット間の衝突判定モード
        /// -1 : 判定なし
        ///  0 / 1 : 判定あり（AABB）
        /// </param>
        public virtual void CameraUpdate(VECTOR targetPosition, VECTOR cameraDistance, float cameraAngleHorizon, float cameraAngleVertical, int CollModelNo = -1)
        {
            // ----------------------------
            // 角度（度）→ ラジアン変換
            // DxLib の回転行列はラジアン指定
            // ----------------------------
            float horizonRadian = Calc.DegreeToRadian(cameraAngleHorizon);
            float verticalRadian = Calc.DegreeToRadian(cameraAngleVertical);
            // ----------------------------
            // 回転行列を作成
            // ・Y軸回転（左右）
            // ・X軸回転（上下）
            // ----------------------------
            MATRIX rotYMatrix = MGetRotY(horizonRadian);
            MATRIX rotXMatrix = MGetRotX(verticalRadian);
            // ----------------------------
            // 回転行列を合成
            // ※ 回転順は「X → Y」
            // ----------------------------
            MATRIX combinedMatrix = MMult(rotXMatrix, rotYMatrix);
            // ----------------------------
            // カメラ距離ベクトルを回転させる
            // これにより
            // ・角度に応じたカメラ位置オフセット
            // が得られる
            // ----------------------------
            VECTOR rotatedOffset = VTransform(cameraDistance, combinedMatrix);
            // ----------------------------
            // 実際のカメラ座標
            // ターゲット位置 + 回転後オフセット
            // ----------------------------
            VECTOR cameraPosition = VAdd(targetPosition, rotatedOffset);

            // ----------------------------
            // カメラとターゲット間の
            // 障害物衝突判定（任意）
            // ----------------------------
            if ((CollModelNo == 0) || (CollModelNo == 1))
            {
                // ターゲット → カメラへの線分と
                // ワールドの AABB との当たり判定
                Physics.COLL_RESULT hit = StClass.DAT.physics.LineVsAABB_WithNormal(targetPosition, cameraPosition);
                // 何かに当たった場合
                if (hit.HitFlag == TRUE)
                {
                    // ターゲットに近すぎる場合
                    // → 最低距離を確保
                    if (hit.Distance < 0.5f)
                    {
                        // Z方向（後方）を最低 0.5 に制限
                        cameraDistance.z = 0.5f;
                        // 再度回転・位置計算
                        rotatedOffset = VTransform(cameraDistance, combinedMatrix);
                        cameraPosition = VAdd(targetPosition, rotatedOffset);
                    }
                    else
                    {
                        // 衝突位置までカメラを前進させる
                        // 少し上にずらして壁に埋まるのを防ぐ
                        cameraPosition = VGet(hit.HitPosition.x, hit.HitPosition.y + 0.1f, hit.HitPosition.z);
                    }
                }
            }

            // ----------------------------
            // カメラを設定
            // ・位置 : cameraPosition
            // ・注視点 : targetPosition
            // ・上方向ベクトル : Y+
            // ----------------------------
            SetCameraPositionAndTarget_UpVecY(cameraPosition, targetPosition);
        }
    }
}
