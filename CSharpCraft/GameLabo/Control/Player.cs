using CmnDxlib;
using ModelLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DX;

namespace GameLabo
{
    public partial class BaseController : IDisposable
    {
        // ジャンプ時に与える初速
        const float jumpPower = 5f;

        /// <summary>
        /// プレイヤー操作全体を制御するメイン処理
        /// ・カメラ操作
        /// ・移動
        /// ・ジャンプ
        /// ・当たり判定
        /// ・モード切替（建築 / ハンター）
        /// </summary>
        public void PlayerController()
        {
            // =========================================
            // ワールド表示範囲（アトラス）の拡張処理
            // =========================================
            StClass.WRLD.atlasViewNum += StClass.loopTime;
            if (StClass.WRLD.atlasViewNum > World.atlasViewNumMax) StClass.WRLD.atlasViewNum = World.atlasViewNumMax;

            // =========================================
            // カメラ距離の切り替え（Oキー）
            // =========================================
            if (StClass.INP.IsKeyPressed(KEY_INPUT_O))
            {
                StClass.CameraDistanceNo = (StClass.CameraDistanceNo + 1) % StClass.CameraDistances.Length;
                StClass.CameraDistance = StClass.CameraDistances[StClass.CameraDistanceNo];
            }

            // プレイヤーモデル情報を取得
            ModelInfo m = StClass.DAT.modelInfo[StClass.UserID];

            // =========================================
            // カメラ回転操作（右スティック or WASD）
            // =========================================
            if ((StClass.INP.JoypadXBuf2 != 0f) || (StClass.INP.JoypadYBuf2 != 0f))
            {
                // 右スティックによるカメラ操作
                StClass.CameraAngleHorizon += ((90.0f / 1000f) * (float)StClass.INP.JoypadXBuf2) * StClass.loopTime;
                StClass.CameraAngleVertical -= ((90.0f / 1000f) * (float)StClass.INP.JoypadYBuf2) * StClass.loopTime;
                // 縦方向の角度制限（真上・真下防止）
                if (StClass.CameraAngleVertical > 89f) StClass.CameraAngleVertical = 89f;
                if (StClass.CameraAngleVertical < -89f) StClass.CameraAngleVertical = -89f;
            }
            else
            {
                // キーボードによるカメラ操作
                if (StClass.INP.IsKeyHeld(KEY_INPUT_A))
                {
                    StClass.CameraAngleHorizon -= (90.0f * StClass.loopTime);
                    StClass.CameraAngleHorizon = (StClass.CameraAngleHorizon + 360f) % 360.0f;
                }
                else if (StClass.INP.IsKeyHeld(KEY_INPUT_D))
                {
                    StClass.CameraAngleHorizon += (90.0f * StClass.loopTime);
                    StClass.CameraAngleHorizon = (StClass.CameraAngleHorizon + 360f) % 360.0f;
                }
                if (StClass.INP.IsKeyHeld(KEY_INPUT_W))
                {
                    StClass.CameraAngleVertical += (90.0f * StClass.loopTime);
                    if (StClass.CameraAngleVertical > 89f) StClass.CameraAngleVertical = 89f;
                }
                else if (StClass.INP.IsKeyHeld(KEY_INPUT_S))
                {
                    StClass.CameraAngleVertical -= (90.0f * StClass.loopTime);
                    if (StClass.CameraAngleVertical < -89f) StClass.CameraAngleVertical = -89f;
                }
            }

            // =========================================
            // 移動入力・アニメーション設定
            // =========================================
            {
                // 初期状態は停止
                m.Speed = 0;

                // 持っているブロックで待機アニメ変更
                if (StClass.DAT.selectBlockID <= 0)
                {
                    m.AnimeIndex = Model.C001_Idle;
                }
                else
                {
                    m.AnimeIndex = Model.C014_TorchIdle;
                }

                // ---------- アナログスティック移動 ----------
                if ((StClass.INP.JoypadXBuf1 != 0f) || (StClass.INP.JoypadYBuf1 != 0f))
                {
                    float pad = (float)(Math.Abs(StClass.INP.JoypadXBuf1) | Math.Abs(StClass.INP.JoypadYBuf1));
                    // 走り／歩き切り替え
                    if (StClass.INP.IsKeyHeld(KEY_INPUT_0))
                    {
                        m.Speed = pad / 1000f * run_speed;
                    }
                    else
                    {
                        m.Speed = pad / 1000f * (run_speed / 2f);
                    }

                    // 入力方向＋カメラ角度から向きを決定
                    float angle = (float)Math.Atan2(-StClass.INP.JoypadXBuf1, StClass.INP.JoypadYBuf1);
                    angle = angle * 180.0f / DX_PI_F;
                    m.Rotation.y = (angle + StClass.CameraAngleHorizon) % 360f;
                }
                // ---------- 十字キー移動 ----------
                else
                {
                    if (StClass.INP.IsKeyHeld(KEY_INPUT_UP) && StClass.INP.IsKeyHeld(KEY_INPUT_LEFT))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (135f + StClass.CameraAngleHorizon) % 360f;
                    }
                    else if (StClass.INP.IsKeyHeld(KEY_INPUT_UP) && StClass.INP.IsKeyHeld(KEY_INPUT_RIGHT))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (225f + StClass.CameraAngleHorizon) % 360f;
                    }
                    else if (StClass.INP.IsKeyHeld(KEY_INPUT_DOWN) && StClass.INP.IsKeyHeld(KEY_INPUT_LEFT))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (45f + StClass.CameraAngleHorizon) % 360f;
                    }
                    else if (StClass.INP.IsKeyHeld(KEY_INPUT_DOWN) && StClass.INP.IsKeyHeld(KEY_INPUT_RIGHT))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (315f + StClass.CameraAngleHorizon) % 360f;
                    }
                    else if (StClass.INP.IsKeyHeld(KEY_INPUT_UP))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (180f + StClass.CameraAngleHorizon) % 360f;
                    }
                    else if (StClass.INP.IsKeyHeld(KEY_INPUT_DOWN))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (0f + StClass.CameraAngleHorizon) % 360f;
                    }
                    else if (StClass.INP.IsKeyHeld(KEY_INPUT_LEFT))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (90f + StClass.CameraAngleHorizon) % 360f;
                    }
                    else if (StClass.INP.IsKeyHeld(KEY_INPUT_RIGHT))
                    {
                        m.Speed = run_speed / 2f;
                        m.Rotation.y = (270f + StClass.CameraAngleHorizon) % 360f;
                    }
                    // Shift押しで走り
                    if (StClass.INP.IsKeyHeld(KEY_INPUT_LSHIFT) && (m.Speed > 0f))
                    {
                        m.Speed = run_speed;
                    }
                }

                // ハンター溜め中や歪み演出中は移動不可
                if ((m.HunterStatus) == 1 || (StClass.ShaderDistortion.IsTargetDist))
                {
                    m.Speed = 0;
                }

                // =========================================
                // ボタン入力状態をモデルに反映
                // =========================================
                m.ButtonA = FALSE;
                if (StClass.INP.IsKeyHeld(KEY_INPUT_Z)) m.ButtonA = TRUE;
                m.ButtonB = FALSE;
                if (StClass.INP.IsKeyHeld(KEY_INPUT_C)) m.ButtonB = TRUE;
                m.ButtonX = FALSE;
                if (StClass.INP.IsKeyHeld(KEY_INPUT_X) || StClass.INP.IsKeyHeld(KEY_INPUT_P)) m.ButtonX = TRUE;
                m.ButtonY = FALSE;
                if (StClass.INP.IsKeyHeld(KEY_INPUT_Y)) m.ButtonY = TRUE;
            }

            // =========================================
            // デバッグ用：ワールド再生成（Uキー）
            // =========================================
            if (StClass.INP.IsKeyPressed(KEY_INPUT_U))
            {
                m.Position.x = 0.5f;
                m.Position.y = 128f;
                m.Position.z = 0.5f;

                StClass.WRLD.neededChunks.Clear();
                StClass.WRLD.pendingChunks.Clear();

                StClass.WRLD.atlasViewNum = 3;
                for (int z = 0; z < StClass.WRLD.atlasViewNum; z++)
                {
                    for (int x = 0; x < StClass.WRLD.atlasViewNum; x++)
                    {
                        StClass.WRLD.MakeAtlas((x - ((int)StClass.WRLD.atlasViewNum / 2), 0, z - ((int)StClass.WRLD.atlasViewNum / 2)));
                    }
                }
                for (int z = 0; z < StClass.WRLD.atlasViewNum; z++)
                {
                    for (int x = 0; x < StClass.WRLD.atlasViewNum; x++)
                    {
                        StClass.WRLD.chunks.CreateVertex((x - ((int)StClass.WRLD.atlasViewNum / 2), 0, z - ((int)StClass.WRLD.atlasViewNum / 2)));
                    }
                }
            }

            // =========================================
            // 移動・ジャンプ・重力処理
            // =========================================
            VECTOR velocity = VGet(0f, 0f, 0f);
            if (m.Speed > 0)
            {
                // 移動アニメ切り替え
                if (m.Speed < run_speed)
                {
                    m.AnimeIndex = Model.C002_Walk;
                }
                else
                {
                    m.AnimeIndex = Model.C003_Run;
                }

                float r = Calc.DegreeToRadian(m.Rotation.y);
                velocity.x = -((float)Math.Sin(r) * m.Speed * StClass.loopTime);
                velocity.z = -((float)Math.Cos(r) * m.Speed * StClass.loopTime);
            }

            // ジャンプ開始
            if (StClass.INP.IsKeyPressed(KEY_INPUT_Z) && m.IsGround && (m.HunterStatus != 1))
            {
                m.verticalVelocity = jumpPower;
                m.isJumping = true;
            }
            else
            {
                // 重力加算
                m.verticalVelocity -= gravity * StClass.loopTime;
            }

            // =========================================
            // 当たり判定付き移動処理
            // =========================================
            VECTOR oldPos = m.Position;
            VECTOR newPos = oldPos;
            // X方向
            newPos.x += velocity.x;
            if (StClass.DAT.physics.IsColliding(newPos, 0.25f, 1.5f))
            {
                newPos.x = oldPos.x;
                velocity.x = 0;
            }
            // Z方向
            newPos.z += velocity.z;
            if (StClass.DAT.physics.IsColliding(newPos, 0.25f, 1.5f))
            {
                newPos.z = oldPos.z;
                velocity.z = 0;
            }
            // Y方向（重力）
            newPos.y += m.verticalVelocity * StClass.loopTime;
            float? groundY = StClass.DAT.physics.GetGroundY(oldPos, newPos, 0.25f);
            if (groundY.HasValue)
            {
                newPos.y = groundY.Value;
                m.verticalVelocity = 0;
                m.IsGround = true;
                m.isJumping = false;
            }
            else
            {
                m.IsGround = false;
            }

            // 高さ制限
            if (newPos.y < 0f)
            {
                newPos.y = 0f;
                m.verticalVelocity = 0f;
                m.IsGround = true;
            }
            else if (newPos.y > 255f)
            {
                newPos.y = 255f;
                m.verticalVelocity = 0f;
            }

            // 位置確定
            m.Position = newPos;

            // =========================================
            // モード切り替え（Space / Enter）
            // =========================================
            if (StClass.INP.IsKeyPressed(KEY_INPUT_SPACE) || StClass.INP.IsKeyPressed(KEY_INPUT_RETURN))
            {
                if (m.Mode == 0)
                {
                    m.Mode = 4000;
                }
                else
                {
                    m.Mode = 0;
                }
            }

            // モデル情報保存
            StClass.DAT.modelInfo[StClass.UserID] = m;

            // =========================================
            // モード別処理
            // =========================================
            switch (StClass.DAT.modelInfo[StClass.UserID].Mode)
            {
                case 0:
                    FieldOperation();
                    break;
                case 4000:
                    HunterOperation();
                    break;
            }
        }
    }
}
