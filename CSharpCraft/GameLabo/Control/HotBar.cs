using System;
using static DX;

namespace GameLabo
{
    public partial class BaseView : IDisposable
    {
        /// <summary>
        /// ホットバーに表示するスロット数
        /// （0番は「削除」用スロット）
        /// </summary>
        const int HOTBAR_SIZE = 11;

        /// <summary>
        /// プレイヤーの手元に
        /// 現在選択中のブロックを表示する
        /// </summary>
        /// <param name="handPos">手元のワールド座標</param>
        public void DrawHandBlock(VECTOR handPos)
        {
            // ブロックIDが 0 以下（削除モード）の場合は
            // 手元に何も表示しない
            if (StClass.DAT.selectBlockID <= 0) return;

            // 手元位置を少し上にずらして
            // 選択中ブロックのビルボードを描画
            DrawBillboard3D(
                VAdd(handPos, VGet(0, 0.4f, 0)),    // 手元からのオフセット
                0.5f,                               // 横サイズ
                0.5f,                               // 縦サイズ
                0.5f,                               // 奥行きサイズ
                0.0f,                               // 回転角
                StClass.DAT.blockIcons[StClass.DAT.selectBlockID],  // 使用テクスチャ
                TRUE);                              // 透明有効
        }

        /// <summary>
        /// 画面下部にホットバー（ブロック選択UI）を描画する
        /// </summary>
        public void DrawHotbar()
        {
            // ---------------------------------
            // 画面サイズ取得
            // ---------------------------------
            int screenW = StClass.GAME_WIDTH;
            int screenH = StClass.GAME_HEIGHT;

            // ---------------------------------
            // ホットバーUI設定
            // ---------------------------------
            int iconSize = 40;  // 各アイコンのサイズ
            int margin = 12;    // アイコン間の余白

            // ホットバー全体の横幅
            int totalWidth = HOTBAR_SIZE * (iconSize + margin) - margin;
            // 画面中央に配置するための開始X座標
            int startX = (screenW - totalWidth) / 2;
            // 画面下部に配置
            int y = screenH - iconSize - 20;

            // ---------------------------------
            // ホットバー各スロット描画
            // ---------------------------------
            for (int i = 0; i < HOTBAR_SIZE; i++)
            {
                // 各スロットのX座標
                int x = startX + i * (iconSize + margin);

                // ---------------------------------
                // 選択中スロットの場合
                // 黄色い強調枠を描画
                // ---------------------------------
                if (StClass.DAT.selectBlockID == i)
                {
                    DrawBox(
                        x - 8, y - 8,
                        x + iconSize + 8, y + iconSize + 8,
                        GetColor(255, 255, 0),  // 黄色
                        TRUE
                    );
                }

                // ---------------------------------
                // スロットの背景枠を描画
                // ---------------------------------
                DrawBox(
                    x - 2, y - 2,
                    x + iconSize + 2, y + iconSize + 2,
                    GetColor(50, 50, 50),   // ダークグレー
                    TRUE
                );

                // ---------------------------------
                // スロット内容描画
                // ---------------------------------
                if (i == 0)
                {
                    // 0番スロットは「削除」専用
                    SetFontSize(16);
                    DrawString(x + 4, y + 10, "掘削", GetColor(255, 255, 255));
                }
                else
                {
                    // 通常ブロックアイコン描画
                    DrawExtendGraph(
                        x, y,
                        x + iconSize, y + iconSize,
                        StClass.DAT.blockIcons[i],
                        TRUE
                    );
                }
            }
        }
    }
}
