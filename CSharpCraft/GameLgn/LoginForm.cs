using GameLabo;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GameLgn
{
    /// <summary>
    /// ゲーム起動前の簡易ログイン（スタート）画面
    /// </summary>
    public partial class LoginForm : Form
    {
        /// <summary>
        /// コンストラクタ
        /// フォームの初期設定を行う
        /// </summary>
        public LoginForm()
        {
            // デザイナで作成した UI の初期化
            InitializeComponent();

            // デフォルトはキャンセル扱い
            // （×ボタンなどで閉じた場合にゲームを開始しないため）
            this.DialogResult = DialogResult.Cancel;

            // 背景画像を設定
            this.BackgroundImage = Image.FromFile(".\\Resources\\Various\\yagi_home.png");

            // 背景画像をフォーム全体に引き伸ばして表示
            this.BackgroundImageLayout = ImageLayout.Stretch;
        }

        /// <summary>
        /// 「Run」ボタン押下時の処理
        /// ゲーム開始を確定する
        /// </summary>
        private void Run_Click(object sender, EventArgs e)
        {
            // 仮のユーザーIDを設定
            // （現状はログイン処理なしのため固定値）
            StClass.UserID = 0;

            // ダイアログ結果を OK に設定
            // 呼び出し元で「ゲーム開始」と判定される
            this.DialogResult = DialogResult.OK;

            // フォームを閉じる
            this.Close();
        }
    }
}
