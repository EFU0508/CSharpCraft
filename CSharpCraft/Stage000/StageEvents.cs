using GameLabo;
using System;

namespace Stage000
{
    /// <summary>
    /// ステージ000用のイベント管理クラス
    /// プレイヤー位置とブロックIDに応じてイベントを発火する
    /// </summary>
    public class StageEvents : BaseEvents
    {
        /// <summary>
        /// イベントID定義
        /// </summary>
        public static readonly int EVENT_000 = 0;
        public static readonly int EVENT_001 = 1;

        /// <summary>
        /// ブロックIDとイベントIDの対応表
        /// [0] = ブロックID
        /// [1] = イベントID
        /// </summary>
        public static readonly int[,] EventIds =
        {
            // 障害物ブロックに乗ったら EVENT_001 を発生させる
            { Chunks.Block_Obs, EVENT_001 },
        };

        /// <summary>
        /// コンストラクタ
        /// ブロックID → イベントID の対応を辞書に登録する
        /// </summary>
        public StageEvents()
        {
            for (int i = 0; i < EventIds.GetLength(0); i++)
            {
                // BaseEvents が持つ EVENTS_IDS にマッピング
                EVENTS_IDS[(ushort)EventIds[i, 0]] = EventIds[i, 1];
            }
        }

        /// <summary>
        /// リソース解放処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

        }

        /// <summary>
        /// 毎フレーム呼ばれるイベント更新処理
        /// </summary>
        public override void Update()
        {
            // 基底クラス側のイベント更新処理
            base.Update();

            // プレイヤーの現在位置（整数ブロック座標）を取得
            (int, int, int) pos = (
                (int)Math.Floor(StClass.DAT.modelInfo[StClass.UserID].Position.x),
                (int)Math.Floor(StClass.DAT.modelInfo[StClass.UserID].Position.y),
                (int)Math.Floor(StClass.DAT.modelInfo[StClass.UserID].Position.z)
                );

            // プレイヤーが立っているブロックIDを取得
            ushort blockId = StClass.WRLD.GetWorldBlockId(pos);

            // 対応するイベントが存在する場合
            if (EVENTS_IDS.ContainsKey(blockId))
            {
                // 次に遷移するステージIDを設定
                StClass.NextStageID = 1;

                // 画面歪み（フェードアウト）開始
                StClass.ShaderDistortion.FadeOut();
            }

            // 歪みシェーダーの進行更新
            StClass.ShaderDistortion.Update(StClass.loopTime);

            // ステージ遷移条件：
            // ・次のステージIDが現在と異なる
            // ・歪みエフェクトが目標値に到達していない
            if ((StClass.NextStageID != StClass.StageID) && !StClass.ShaderDistortion.IsTargetDist)
            {
                // 現ステージのループを抜ける
                StClass.isStayStage = false;
            }
        }

        /// <summary>
        /// 表示処理（現状は基底クラス処理のみ）
        /// </summary>
        public override void Show()
        {
            base.Show();

        }
    }
}
