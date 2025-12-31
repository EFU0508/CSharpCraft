using CmnDxlib;
using GameLabo;
using System;
using System.IO;
using System.Windows.Forms;
using static DX;

namespace Scenario
{
    /// <summary>
    /// ゲーム起動・DxLib初期化クラス
    /// ・ウィンドウ設定
    /// ・DxLib 初期化
    /// ・共通シングルトン（StClass）初期化
    /// </summary>
    public class GameStart : IDisposable
    {
        /// <summary>
        /// コンストラクタ
        /// ゲーム起動時に一度だけ呼ばれる
        /// </summary>
        public GameStart()
        {
            // =====================================
            // ウィンドウサイズ設定
            // =====================================

            // 実際のディスプレイ解像度を取得
            StClass.WINDOW_WIDTH = Screen.PrimaryScreen.Bounds.Width;
            StClass.WINDOW_HEIGHT = Screen.PrimaryScreen.Bounds.Height;
            {
                // 画面の 90% サイズでウィンドウを作る
                //（10分割して9分割分を使用）
                int WidthRatio = StClass.WINDOW_WIDTH / 10;
                int HeightRatio = StClass.WINDOW_HEIGHT / 10;
                StClass.WINDOW_WIDTH = WidthRatio * 9;
                StClass.WINDOW_HEIGHT = HeightRatio * 9;

                // ゲーム内部解像度（描画解像度）
                StClass.GAME_WIDTH = 1280;
                StClass.GAME_HEIGHT = 720;
            }

            // 実行ファイル名をウィンドウタイトルに設定
            StClass.Title = Path.GetFileNameWithoutExtension(Application.ExecutablePath);

            // =====================================
            // DxLib 基本設定
            // =====================================
#if DEBUG
            // デバッグ時は DxLib のログ出力を有効
            SetOutApplicationLogValidFlag(TRUE);
#else
            // リリース時はログを出さない
            SetOutApplicationLogValidFlag(FALSE);
#endif
            // Direct3D 11 を使用
            SetUseDirect3DVersion(DX_DIRECT3D_11);

            // ウィンドウ設定
            SetWindowTextW(StClass.Title);  // タイトル
            SetWindowIconHandle(CSharpCraft.Properties.Resources.usochan_icon.Handle);  // アイコン
            SetWindowStyleMode(7);  // 標準ウィンドウスタイル
            SetGraphMode(StClass.GAME_WIDTH, StClass.GAME_HEIGHT, 32);  // 描画解像度
            SetWindowSize(StClass.WINDOW_WIDTH, StClass.WINDOW_HEIGHT); // 表示サイズ
            ChangeWindowMode(TRUE); // ウィンドウモード

            // =====================================
            // DxLib 初期化
            // =====================================

            int ret = DxLib_Init();
            if (ret < 0)
            {
                // 初期化失敗時は即終了
                MessageBox.Show("DxLib_Init Error");
                Application.Exit();
            }

            // =====================================
            // DxLib 動作オプション
            // =====================================

            SetUseLighting(TRUE);                       // ライティング有効
            SetUseDirectInputFlag(TRUE);                // DirectInput 使用
            SetDirectInputMouseMode(FALSE);             // マウス非排他
            SetWindowSizeChangeEnableFlag(TRUE, TRUE);  // ウィンドウサイズ変更許可
            SetUsePixelLighting(TRUE);                  // ピクセルライティング有効
            SetFullSceneAntiAliasingMode(4, 2);         // フルスクリーンAA（4x）
            SetEnableXAudioFlag(TRUE);                  // XAudio2 使用
            Set3DSoundOneMetre(1.0f);                   // 3Dサウンド基準距離
            SetWaitVSyncFlag(FALSE);                    // VSync 無効（FPS制御は自前）
            SetAlwaysRunFlag(TRUE);                     // 非アクティブ時も動作
            SetUseDXArchiveFlag(TRUE);                  // DXA アーカイブ使用
            SetWindowUserCloseEnableFlag(TRUE);         // ウィンドウ×ボタン有効

            // 描画先をバックバッファに設定
            SetDrawScreen(DX_SCREEN_BACK);

            // =====================================
            // Zバッファ・カメラ設定
            // =====================================

            SetUseZBufferFlag(TRUE);                    // Zバッファ使用
            SetWriteZBufferFlag(TRUE);                  // Z書き込み有効
            SetCameraNearFar(0.1f, 1000.0f);            // 近・遠クリップ
            SetZBufferBitDepth(24);                     // Zバッファ精度

            // 画面モード変更時にグラフィックリセットしない
            SetChangeScreenModeGraphicsSystemResetFlag(FALSE);

            // =====================================
            // カメラ初期状態
            // =====================================

            StClass.CameraAngleHorizon = 0.0f;
            StClass.CameraAngleVertical = 0.0f;
            StClass.CameraDistanceNo = 0;
            StClass.CameraDistance = StClass.CameraDistances[StClass.CameraDistanceNo];

            // =====================================
            // ゲーム共通オブジェクト初期化
            // =====================================

            StClass.INP = new Input(false);     // 入力管理
            StClass.DAT = new GameData();       // ゲームデータ
            StClass.NPC = new NpcManager();     // NPC管理
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void Dispose()
        {
            // ゲームループ停止
            StClass.isRunning = false;

            // 共通オブジェクト破棄
            StClass.NPC?.Dispose();
            StClass.NPC = null;
            StClass.DAT?.Dispose();
            StClass.DAT = null;
            StClass.INP?.Dispose();
            StClass.INP = null;
        }

        /// <summary>
        /// ゲーム本体実行
        /// </summary>
        public void Run()
        {
            // シーン管理クラスに処理を委譲
            using (Scene scene = new Scene())
            {
                scene.Run();
            }
        }
    }
}
