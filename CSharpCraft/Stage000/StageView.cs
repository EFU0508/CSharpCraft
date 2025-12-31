using GameLabo;
using ModelLib;
using System;
using static CmnDxlib.Func;
using static DX;

namespace Stage000
{
    /// <summary>
    /// ステージ000の描画処理を担当するクラス
    /// ・カメラ制御
    /// ・ワールド描画
    /// ・プレイヤー / NPC 描画
    /// ・UI / デバッグ表示
    /// ・画面歪み（フェード）演出
    /// </summary>
    public class StageView : BaseView
    {
        /// <summary>
        /// コンストラクタ
        /// ステージ開始時にフェードイン（歪み解除）を開始する
        /// </summary>
        public StageView()
        {
            StClass.ShaderDistortion.FadeIn();
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// ステージの描画処理（1フレーム分）
        /// </summary>
        public override void Draw()
        {
            // ==========================
            // 描画状態の初期化
            // ==========================

            // 描画先をバックバッファに設定
            SetDrawScreen(DX_SCREEN_BACK);

            // Zバッファ有効化（3D描画用）
            SetUseZBufferFlag(TRUE);
            SetWriteZBufferFlag(TRUE);

            // カメラの描画範囲設定
            SetCameraNearFar(0.1f, 1000.0f);

            // Zバッファ精度
            SetZBufferBitDepth(24);

            // ==========================
            // 背景クリア
            // ==========================

            // 時刻に応じた背景色更新
            UpdateBackgroundColor(StClass.virtualTime);

            // 画面クリア
            ClearDrawScreen();

            // ==========================
            // フェード（歪み）演出中の処理
            // ==========================
            if (StClass.ShaderDistortion.IsTargetDist)
            {
                // 歪み用シェーダーを有効化
                StClass.ShaderDistortion.SetShaderMode();

                // 歪み中も背景色を再設定
                UpdateBackgroundColor(StClass.virtualTime);
                ClearDrawScreen();
            }

            // ==========================
            // 共通描画（BaseView）
            // ==========================

            base.Draw();

            // ==========================
            // カメラ更新
            // ==========================

            // プレイヤー頭部付近を注視点に設定
            base.CameraUpdate(VAdd(StClass.DAT.modelInfo[StClass.UserID].Position, VGet(0, 1.5f, 0)), StClass.CameraDistance, StClass.CameraAngleHorizon, StClass.CameraAngleVertical, StClass.CameraDistanceNo);

            // ==========================
            // ライティング更新（太陽・月）
            // ==========================

            UpdateDirectionalLight(StClass.DAT.modelInfo[StClass.UserID].Position, StClass.DAT.SunHandle, StClass.virtualTime, -1, StClass.DAT.MoonHandle);

            // ==========================
            // ワールド描画
            // ==========================

            // プレイヤー周辺チャンクのみ描画
            StClass.WRLD.DrawAroundPlayer(StClass.DAT.modelInfo[StClass.UserID].Position);

            // NPC描画
            {
                StClass.NPC.Draw();
            }

            // ==========================
            // プレイヤー描画
            // ==========================

            if (StClass.DAT.modelInfo.ContainsKey(StClass.UserID) && (StClass.DAT.modelInfo[StClass.UserID] != null))
            {
                ModelInfo m = StClass.DAT.modelInfo[StClass.UserID];

                // 一人称視点時は自キャラを描画しない
                if (!(StClass.CameraDistanceNo == 2))
                {
                    StClass.DAT.model.Draw(m);
                }

                // ==========================
                // 通常モード（建築・探索）
                // ==========================

                if (m.Mode == 0)
                {
                    // 左手のフレーム位置を取得
                    VECTOR rightHandPos = MV1GetFramePosition(
                    m.Handle,
                    StClass.DAT.model.BaseModel[m.ModelType].LeftHand);

                    // 手元に持っているブロック描画
                    DrawHandBlock(rightHandPos);

                    // エイム表示
                    StClass.WRLD.DrawAim();

                    // ホットバーUI描画
                    DrawHotbar();
                }
                // ==========================
                // ハンターモード（弓）
                // ==========================
                else
                {
                    {
                        // 弓モデル描画
                        MV1SetPosition(StClass.DAT.model.Bow_WoodHandle, m.BowPosition);
                        MV1SetRotationXYZDegree(StClass.DAT.model.Bow_WoodHandle, m.BowRotation);
                        MV1DrawModel(StClass.DAT.model.Bow_WoodHandle);
                    }

                    {
                        // 矢の状態に応じた描画
                        if ((m.HunterStatus == 0) || (m.HunterStatus == 2))
                        {
                            // 通常状態
                            MV1SetRotationXYZDegree(m.ArrowHandle, m.ArrowRotation);
                        }
                        else if (m.HunterStatus == 1)
                        {
                            // エイム中（照準表示）
                            MV1SetRotationXYZDegree(m.ArrowHandle, m.ArrowRotation);

                            const int size = 10;
                            uint color1 = GetColor(255, 255, 255);
                            uint color0 = GetColor(0, 0, 0);
                            VECTOR Center = ConvWorldPosToScreenPos(m.ArrowMidpoint);
                            int CenterX = (int)Center.x;
                            int CenterY = (int)Center.y;

                            // 照準（十字）描画
                            DrawLine(CenterX + 1, CenterY - size + 1, CenterX + 1, CenterY + size + 1, color0);
                            DrawLine(CenterX - size + 1, CenterY + 1, CenterX + size + 1, CenterY + 1, color0);
                            DrawLine(CenterX, CenterY - size, CenterX, CenterY + size, color1);
                            DrawLine(CenterX - size, CenterY, CenterX + size, CenterY, color1);
                        }

                        // 矢モデル描画
                        MV1SetPosition(m.ArrowHandle, m.ArrowPosition);
                        MV1DrawModel(m.ArrowHandle);
                    }
                }
            }

            // ==========================
            // UI / デバッグ表示
            // ==========================

            SetFontSize(32);

            // ゲーム内時刻表示
            string hourStr = StClass.virtualTime.Hours.ToString("D2");
            string miniStr = (StClass.virtualTime.Minutes / 10 * 10).ToString("D2");
            DrawString(StClass.GAME_WIDTH - 90, 0, $"{hourStr}:{miniStr}", GetColor(255, 255, 255));

            // チャンク生成待ち数
            DrawFormatString(0, StClass.GAME_HEIGHT - 70, GetColor(255, 255, 255), "Queueing:{0}", StClass.WRLD.pendingChunks.Count);

            // プレイヤー座標表示
            int x = (int)Math.Floor(StClass.DAT.modelInfo[StClass.UserID].Position.x);
            int y = (int)Math.Floor(StClass.DAT.modelInfo[StClass.UserID].Position.y);
            int z = (int)Math.Floor(StClass.DAT.modelInfo[StClass.UserID].Position.z);
            DrawFormatString(0, StClass.GAME_HEIGHT - 40, GetColor(255, 255, 255), "X:{0} Y:{1} Z:{2}", x, y, z);

            // ==========================
            // 歪みシェーダー最終描画
            // ==========================
            if (StClass.ShaderDistortion.IsTargetDist)
            {
                StClass.ShaderDistortion.SetShaderDraw(0.0f, StClass.GAME_WIDTH, 0.0f, StClass.GAME_HEIGHT, StClass.ShaderDistortion.DistPower);
            }
        }
    }
}