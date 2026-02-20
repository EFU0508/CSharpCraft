using System;
using System.Windows.Forms;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// Controllerのベースクラス
    /// </summary>
    public partial class BaseController : IDisposable
    {
        public const float run_speed = 9f;
        public const float gravity = 9.8f;

        private int FPS;
        private int FrameCount;
        private long BaseTime;

        public BaseController()
        {
            BaseTime = GetNowHiPerformanceCount();
            FPS = 0;
            FrameCount = 0;
        }

        public virtual void Dispose()
        {

        }

        public virtual void Update()
        {
            // DxLib のメッセージ処理
            // ウィンドウの×ボタンなどが押されると 0 以外が返る
            if (ProcessMessage() != 0)
            {
                StClass.isRunning = false;  // ゲーム終了フラグ
            }

            float nowtime = StClassLib.GetBaseCount() / 1000.0f;
            // 前フレームとの差分時間（秒）
            StClass.loopTime = nowtime - StClass.lastTime;
            // 次フレーム用に保存
            StClass.lastTime = nowtime;

            // 仮想時間（昼夜サイクル用）
            DateTime now = DateTime.Now;
            double realSecondsToday = now.TimeOfDay.TotalSeconds;
            // 実時間 ×720 → ゲーム内では 20分で1日進む
            double virtualSecondsToday = (realSecondsToday * 720) % 86400;
            // 仮想時刻として保存
            StClass.virtualTime = TimeSpan.FromSeconds(virtualSecondsToday);

            // 入力更新
            StClass.INP.Update();

            // ESCキーで終了確認
            if (CheckHitKey(KEY_INPUT_ESCAPE) == TRUE)
            {
                DialogResult dr = MessageBox.Show("ゲームを終了しますか？", "CSharpCraft", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.OK)
                {
                    StClass.isRunning = false;
                }
            }

            // F1キーでウィンドウモード切替
            // 押しっぱなし防止用フラグあり
            if (CheckHitKey(KEY_INPUT_F1) == TRUE)
            {
                if (!StClass.WindowModeWait)
                {
                    if (StClass.WindowMode)
                    {
                        ChangeWindowMode(TRUE);     // ウィンドウモード
                        SetMouseDispFlag(TRUE);     // マウス表示
                    }
                    else
                    {
                        ChangeWindowMode(FALSE);    // フルスクリーン
                        SetMouseDispFlag(FALSE);    // マウス非表示
                    }
                    StClass.WindowMode = !StClass.WindowMode;
                    StClass.WindowModeWait = true;  // 押しっぱなし防止
                }
            }
            else
            {
                StClass.WindowModeWait = false;
            }

            // FPS 表示用処理
            // ウィンドウタイトルに FPS 表示
            SetMainWindowText(string.Format("{0} {1}FPS", StClass.Title, FPS));
            FrameCount++;
            // 高精度タイマ（マイクロ秒）
            long time = GetNowHiPerformanceCount();
            // 1秒経過したら FPS を更新
            if (time - BaseTime > 1000000)
            {
                FPS = FrameCount;   // この1秒間のフレーム数
                FrameCount = 0;
                BaseTime = time;
            }
        }
    }
}
