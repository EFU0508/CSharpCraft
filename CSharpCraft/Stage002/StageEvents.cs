using GameLabo;
using static DX;

namespace Stage002
{
    /// <summary>
    /// ステージ002用のイベント管理クラス
    /// プレイヤー位置とブロックIDに応じてイベントを発火する
    /// </summary>
    public class StageEvents : BaseEvents
    {
        /// <summary>
        /// イベントID定義
        /// </summary>
        public static readonly int EVENT_000 = 0;

        /// <summary>
        /// ブロックIDとイベントIDの対応表
        /// [0] = ブロックID
        /// [1] = イベントID
        /// </summary>
        public static readonly int[,] EventIds =
        {
        };

        int evt_y = 0;
        int evt_c = 0;

        private readonly string[] EVENT_STR = {
            "C# + DXライブラリ",
            "ぷろぐらまー　Efu0508",
            "でざいなー　ニート",
            "素材　がんばった",
            "こんぐらいちょれーじゃん♪",
            };

        public StageEvents()
        {
            for (int i = 0; i < EventIds.GetLength(0); i++)
            {
                EVENTS_IDS[(ushort)EventIds[i, 0]] = EventIds[i, 1];
            }

            evt_c = 0;
            evt_y = StClass.GAME_HEIGHT / 2;
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

            if (evt_y < 0)
            {
                evt_c++;
                if (evt_c >= EVENT_STR.Length) evt_c = 0;
                evt_y = StClass.GAME_HEIGHT / 2;
            }
            else
            {
                evt_y--;
            }
        }

        /// <summary>
        /// 表示処理（現状は基底クラス処理のみ）
        /// </summary>
        public override void Show()
        {
            base.Show();

            SetFontSize(48);
            DrawString(350, evt_y, EVENT_STR[evt_c], GetColor(255, 255, 255));
        }
    }
}
