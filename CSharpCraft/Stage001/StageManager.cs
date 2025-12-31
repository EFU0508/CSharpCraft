using GameLabo;
using System.Diagnostics;
using System.Threading;
using static DX;

namespace Stage001
{
    /// <summary>
    /// ステージ001全体を管理するマネージャークラス
    /// Controller / View / Events / World を統括し、
    /// ステージのメインループを実行する
    /// </summary>
    public class StageManager : BaseManager
    {
        /// <summary>
        /// コンストラクタ
        /// ステージで使用する各管理クラスを生成・登録する
        /// </summary>
        public StageManager()
        {
            // プレイヤー操作・ロジック担当
            StClass.CTRL = new StageController();

            // 描画担当
            StClass.VIEW = new StageView();

            // イベント処理担当
            StClass.EVNT = new StageEvents();

            // ワールド（ブロック・チャンク管理）
            StClass.WRLD = new World();
        }

        /// <summary>
        /// ステージ終了時のリソース解放処理
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            // 生成順と逆順で破棄
            StClass.WRLD?.Dispose();
            StClass.WRLD = null;
            StClass.EVNT?.Dispose();
            StClass.EVNT = null;
            StClass.VIEW?.Dispose();
            StClass.VIEW = null;
            StClass.CTRL?.Dispose();
            StClass.CTRL = null;
        }

        /// <summary>
        /// ステージのメイン処理ループ
        /// </summary>
        public override void Run()
        {
            base.Run();

            // ワールド（地形・チャンク）の初期化
            StClass.WRLD.InitWorld();

            // 1フレームの目標時間（約30FPS）
            const int targetFrameMs = 33;

            // ステージが有効な間ループ
            while (StClass.isRunning && StClass.isStayStage)
            {
                // フレーム開始時間計測
                Stopwatch sw = Stopwatch.StartNew();

                // ==========================
                // 更新処理
                // ==========================

                // 入力・プレイヤー・モデル更新
                StClass.CTRL.Update();

                // イベント判定・遷移処理
                StClass.EVNT.Update();

                // ==========================
                // 描画処理
                // ==========================

                // ステージ描画
                StClass.VIEW.Draw();

                // イベント用描画（フェード等）
                StClass.EVNT.Show();

                // バックバッファを画面に反映
                ScreenFlip();

                // フレーム処理時間を計測
                sw.Stop();
                int elapsed = (int)sw.ElapsedMilliseconds;

                // ==========================
                // フレームレート制御
                // ==========================

                // 目標フレーム時間に満たない場合はスリープ
                int sleepTime = targetFrameMs - elapsed;
                if (sleepTime > 0)
                {
                    Thread.Sleep(sleepTime);
                }

                // ==========================
                // BGM管理
                // ==========================

                // 再生中のBGMが終了していたら次の曲をランダム再生
                if (CheckSoundMem(StClass.DAT.mp3_Field[StClass.DAT.mp3_Field_Number]) == FALSE)
                {
                    StClass.DAT.mp3_Field_Number = GetRand(StClass.DAT.mp3_Field.Length - 1);
                    PlaySoundMem(StClass.DAT.mp3_Field[StClass.DAT.mp3_Field_Number], DX_PLAYTYPE_BACK);
                }
            }
        }
    }
}
