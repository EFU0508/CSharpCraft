using System;
using Scenario;
using System.Windows.Forms;
using GameLgn;

namespace MinePacraft
{
    /// <summary>
    /// アプリケーションのエントリーポイント
    /// ・多重起動防止
    /// ・ログイン画面表示
    /// ・ゲーム本体起動
    /// ・再起動制御
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// アプリケーションのエントリーポイント
        /// ・多重起動防止
        /// ・ログイン画面表示
        /// ・ゲーム本体起動
        /// ・再起動制御
        /// </summary>
        [STAThread]

        static void Main(string[] args)
        {
            // =====================================
            // 多重起動防止用 Mutex の作成
            // 実行ファイル名を Mutex 名にすることで
            // 同一アプリの重複起動を防ぐ
            // =====================================
            string path = System.Windows.Forms.Application.ExecutablePath;
            string mutexName = System.IO.Path.GetFileNameWithoutExtension(path);
            System.Threading.Mutex mutex = new System.Threading.Mutex(false, mutexName);
            bool hasHandle = false;

            try
            {
                // =====================================
                // Mutex の取得を試みる
                // =====================================
                try
                {
                    // 0ms で即時判定（待たない）
                    hasHandle = mutex.WaitOne(0, false);
                }
                catch (System.Threading.AbandonedMutexException)
                {
                    // 前回異常終了していた場合でも続行可能
                    hasHandle = true;
                }
                // =====================================
                // 既に起動中なら終了
                // =====================================
                if (hasHandle == false)
                {
                    MessageBox.Show("多重起動はできません。");
                    return;
                }

                // =====================================
                // ログインフォーム表示
                // =====================================
                Form form = new LoginForm();
                DialogResult dr = form.ShowDialog();

                // フォームは即破棄
                form.Dispose();
                form = null;

                // =====================================
                // ログイン成功時のみゲーム起動
                // =====================================
                if (dr == DialogResult.OK)
                {
                    using (GameStart gameStart = new GameStart())
                    {
                        // ゲームのメインループ開始
                        gameStart.Run();
                    }
                }

                // =====================================
                // 再起動フラグが立っていれば
                // 自分自身を再実行
                // =====================================
                if (restarting)
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath);
                }
            }
            finally
            {
                // =====================================
                // Mutex の解放と破棄
                // =====================================
                if (hasHandle)
                {
                    mutex.ReleaseMutex();
                }
                mutex.Close();
            }
        }

        /// <summary>
        /// 再起動要求フラグ
        /// true の場合、終了後にアプリを再起動する
        /// </summary>
        private static bool restarting = false;

        /// <summary>
        /// アプリケーションを再起動するためのヘルパー
        /// ・フラグを立てる
        /// ・通常の終了処理を行う
        /// </summary>
        public static void RestartApplication()
        {
            restarting = true;
            Application.Exit();
        }
    }
}
