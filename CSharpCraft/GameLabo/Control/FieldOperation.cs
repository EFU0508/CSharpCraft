using ModelLib;
using System;
using static DX;

namespace GameLabo
{
    public partial class BaseController : IDisposable
    {
        /// <summary>
        /// フィールド（ブロック）操作処理
        /// ・ブロック選択切り替え
        /// ・ブロック破壊
        /// ・ブロック設置
        /// を担当する
        /// </summary>
        public void FieldOperation()
        {
            // ---------------------------------
            // 現在操作中のプレイヤーモデル情報を取得
            // ---------------------------------
            ModelInfo m = StClass.DAT.modelInfo[StClass.UserID];

            // ---------------------------------
            // Qキーが押されたら
            // ブロック選択IDを次へ切り替える
            // ---------------------------------
            if (StClass.INP.IsKeyPressed(KEY_INPUT_Q))
            {
                StClass.DAT.selectBlockID++;
                // ブロックIDが最大数を超えたら
                // 先頭（0）に戻す
                if (StClass.DAT.selectBlockID > 10) StClass.DAT.selectBlockID = 0;
            }

            // ---------------------------------
            // Xボタン（攻撃／使用ボタン）が押されていて
            // なおかつ
            // ・破壊音
            // ・設置音
            // が再生中でない場合のみ処理
            // （連打・多重再生防止）
            // ---------------------------------
            if ((m.ButtonX == TRUE) && (CheckSoundMem(StClass.DAT.mp3_Scrape) == FALSE) && (CheckSoundMem(StClass.DAT.mp3_Build) == FALSE))
            {
                // ---------------------------------
                // 選択ブロックIDが 0 の場合
                // → ブロック破壊モード
                // ---------------------------------
                if (StClass.DAT.selectBlockID == 0)
                {
                    // プレイヤーが向いている先のブロックを削除
                    // 成功すると座標が返る
                    (int, int, int)? pos = StClass.WRLD.RemoveBlock();
                    // 実際にブロックを削除できた場合のみ
                    if (pos.HasValue)
                    {
                        // 破壊音を再生
                        StopSoundMem(StClass.DAT.mp3_Scrape);
                        PlaySoundMem(StClass.DAT.mp3_Scrape, DX_PLAYTYPE_BACK);
                    }
                }
                // ---------------------------------
                // 選択ブロックIDが 1 以上の場合
                // → ブロック設置モード
                // ---------------------------------
                else
                {
                    // プレイヤー位置を基準にブロックを設置
                    // 成功すると設置した座標が返る
                    (int, int, int)? pos = StClass.WRLD.PlaceBlock(StClass.DAT.selectBlockID, m.Position);
                    // 実際にブロックを設置できた場合のみ
                    if (pos.HasValue)
                    {
                        // 設置音を再生
                        StopSoundMem(StClass.DAT.mp3_Build);
                        PlaySoundMem(StClass.DAT.mp3_Build, DX_PLAYTYPE_BACK);
                    }
                }
            }

            // ---------------------------------
            // 更新したプレイヤーモデル情報を
            // グローバル管理データへ戻す
            // ---------------------------------
            StClass.DAT.modelInfo[StClass.UserID] = m;
        }
    }
}
