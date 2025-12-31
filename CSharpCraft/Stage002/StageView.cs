using GameLabo;
using System;
using static CmnDxlib.Func;
using static DX;

namespace Stage002
{
    /// <summary>
    /// ステージ002の描画処理を担当するクラス
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
            SetDrawBlendMode(DX_BLENDMODE_ALPHA, 255);
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            SetDrawBlendMode(DX_BLENDMODE_NOBLEND, 0);
        }

        /// <summary>
        /// ステージの描画処理（1フレーム分）
        /// </summary>
        public override void Draw()
        {
            // ==========================
            // 共通描画（BaseView）
            // ==========================

            base.Draw();

            // ==========================
            // カメラ更新
            // ==========================

            // プレイヤー頭部付近を注視点に設定
            base.CameraUpdate(VAdd(VGet(8, 4, 0), VGet(0, 1.5f, 0)), VGet(0, 0, -10), 0, 0, -1);

            // ==========================
            // ライティング更新
            // ==========================

            UpdateDirectionalLight(VGet(0, 0, 0), -1, new DateTime(1900, 1, 1, 19, 10, 0), -1, -1);

            // NPC描画
            {
                StClass.NPC.Draw();
            }

            if (StClass.WRLD.chunks.dicChunks.ContainsKey((0, 0, 0)))
            {
                DrawPolygon3D_UseVertexBuffer(StClass.WRLD.chunks.dicChunks[(0, 0, 0)].vertexBufferHandle,
                                                StClass.DAT.blockTextures, TRUE);
            }
        }
    }
}
