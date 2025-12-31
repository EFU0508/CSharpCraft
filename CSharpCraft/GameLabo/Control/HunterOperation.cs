using CmnDxlib;
using ModelLib;
using System;
using static DX;

namespace GameLabo
{
    public partial class BaseController : IDisposable
    {
        /// <summary>
        /// ハンター（弓キャラ）の操作処理
        /// ・溜め
        /// ・発射
        /// ・矢の移動
        /// ・アニメ制御
        /// </summary>
        public void HunterOperation()
        {
            // プレイヤー自身のモデル情報を取得
            ModelInfo m = StClass.DAT.modelInfo[StClass.UserID];

            // =========================================
            // 弓を引き始める処理
            // =========================================
            if ((m.ButtonX == TRUE) && (!m.isJumping))
            {
                // 矢が存在していない場合のみ溜め動作へ
                if (m.ArrowAlive == FALSE)
                {
                    m.HunterStatus = 1; // 溜め状態
                    m.AnimeIndex = 7;   // 弓を引くアニメ
                }
            }
            // =========================================
            // 溜め状態中の処理
            // =========================================
            else if (m.HunterStatus == 1)
            {
                if (m.ButtonX == TRUE)
                {
                    // ボタンを押し続けている間は溜め継続
                    m.AnimeIndex = 7;
                }
                else
                {
                    // ボタンを離した瞬間に矢を発射
                    StopSoundMem(StClass.DAT.mp3_Cob);
                    PlaySoundMem(StClass.DAT.mp3_Cob, DX.DX_PLAYTYPE_BACK);

                    m.ArrowAlive = TRUE;    // 矢を有効化
                    m.HunterStatus = 0;     // 通常状態へ
                    m.AnimeIndex = 0;       // 通常アニメへ
                }
            }

            // =========================================
            // 溜め中・発射中は移動を停止
            // =========================================
            if ((m.HunterStatus != 0) && (m.HunterStatus != 2))
            {
                m.Speed = 0;
            }

            // =========================================
            // 弓の位置・回転更新
            // =========================================
            {
                // 左手のフレーム位置に弓を追従させる
                m.BowPosition = MV1GetFramePosition(m.Handle, StClass.DAT.model.BaseModel[m.ModelType].LeftHandMiddle1);
                // 通常時・リロード時は少し傾ける
                if ((m.HunterStatus == 0) || (m.HunterStatus == 2))
                {
                    m.BowRotation = VGet(m.Rotation.x + 45f, m.Rotation.y, m.Rotation.z);
                }
                else
                {
                    // 溜め中は体の回転にそのまま合わせる
                    m.BowRotation = m.Rotation;
                }
            }

            // =========================================
            // 矢の位置・移動制御
            // =========================================
            {
                // 通常状態 or リロード状態 かつ 矢が未発射
                if (((m.HunterStatus == 0) || (m.HunterStatus == 2)) && (m.ArrowAlive == FALSE))
                {
                    // 右手に矢を持たせる
                    m.ArrowPosition = MV1GetFramePosition(m.Handle, StClass.DAT.model.BaseModel[m.ModelType].RightHandMiddle1);
                    m.ArrowRotation = m.Rotation;
                }
                // 溜め状態
                else if (m.HunterStatus == 1)
                {
                    // 右手に矢をセット（逆向き）
                    m.ArrowPosition = MV1GetFramePosition(m.Handle, StClass.DAT.model.BaseModel[m.ModelType].RightHandMiddle1);
                    m.ArrowRotation = VGet(m.Rotation.x, m.Rotation.y + 180f, m.Rotation.z);
                    // 発射方向を計算
                    float angleY = Calc.DegreeToRadian(m.Rotation.y + 180f);
                    VECTOR direction = VGet((float)Math.Sin(angleY), 0, (float)Math.Cos(angleY));
                    // 矢の目標地点（放物線などは使わず直線）
                    m.ArrowMidpoint = VAdd(VGet(m.Position.x, m.Position.y + (m.Height / 2f), m.Position.z), VScale(direction, 30.0f));
                }
                // 矢が飛んでいる最中
                else if (m.ArrowAlive == TRUE)
                {
                    VECTOR? newArrowPosition = ArrowMove(m.ArrowPosition, m.ArrowMidpoint, 20f, StClass.loopTime);
                    // 目標地点に到達したら終了
                    if (newArrowPosition == null)
                    {
                        m.Mode = 4000;
                        m.ArrowAlive = FALSE;
                        m.PreyType = 0;
                        m.PreyScore = 0;
                    }
                    else
                    {
                        // 矢を前進させる
                        m.ArrowPosition = newArrowPosition.Value;
                    }
                }
            }

            // =========================================
            // モデルアニメーション更新
            // =========================================
            StClass.DAT.model.PlayAnimeModel(ref m, StClass.loopTime);
            
            // モデル情報を書き戻す
            StClass.DAT.modelInfo[StClass.UserID] = m;
        }

        /// <summary>
        /// 矢を a から b に向かって移動させる
        /// 到達した場合は null を返す
        /// </summary>
        private VECTOR? ArrowMove(VECTOR a, VECTOR b, float speed, float deltaTime)
        {
            // 移動方向ベクトル
            VECTOR direction = VSub(b, a);
            // 残り距離
            float distance = VSize(direction);
            // 今フレームで到達する場合
            if (distance < (speed * deltaTime))
            {
                return null;
            }
            // 正規化して一定速度で前進
            VECTOR normalizedDirection = VNorm(direction);
            return VAdd(a, VScale(normalizedDirection, speed * deltaTime));
        }
    }
}
