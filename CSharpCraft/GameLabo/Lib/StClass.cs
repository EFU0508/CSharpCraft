using CmnDxlib;
using System;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// ゲーム全体で共有されるステート（状態）管理クラス
    /// </summary>
    public static class StClass
    {
        // ============================
        // 画面・描画設定関連
        // ============================

        /// <summary>
        /// ウィンドウタイトル文字列
        /// </summary>
        public static string Title;

        /// <summary>
        /// 実際のウィンドウサイズ
        /// </summary>
        public static int WINDOW_WIDTH;
        public static int WINDOW_HEIGHT;

        /// <summary>
        /// ゲーム内部で使用する描画解像度
        /// </summary>
        public static int GAME_WIDTH;
        public static int GAME_HEIGHT;

        // ============================
        // カメラ関連
        // ============================

        /// <summary>
        /// カメラ水平回転角（度）
        /// </summary>
        public static float CameraAngleHorizon;

        /// <summary>
        /// カメラ垂直回転角（度）
        /// </summary>
        public static float CameraAngleVertical;

        /// <summary>
        /// カメラと注視点の距離ベクトル
        /// </summary>
        public static VECTOR CameraDistance;

        /// <summary>
        /// カメラ距離プリセットのインデックス
        /// </summary>
        public static int CameraDistanceNo;

        /// <summary>
        /// カメラ距離プリセット（切り替え用）
        /// </summary>
        public static readonly VECTOR[] CameraDistances = new VECTOR[]
        {
            VGet(0, 0, -10f),   // 通常視点
            VGet(0, 0, -20f),   // 遠景視点
            VGet(0, 0, -0.1f),  // 一人称視点
        };

        /// <summary>
        /// 正射影時の表示範囲
        /// </summary>
        public static VECTOR OrthoViewArea;

        /// <summary>
        /// カメラ用正射影エリア
        /// </summary>
        public static VECTOR CameraOrthoArea;

        /// <summary>
        /// 正射影エリアのスケール
        /// </summary>
        public static float OrthoArea;

        // ============================
        // ウィンドウモード管理
        // ============================

        /// <summary>
        /// ウィンドウモードかどうか
        /// </summary>
        public static bool WindowMode = false;

        /// <summary>
        /// ウィンドウモード切り替え連打防止フラグ
        /// </summary>
        public static bool WindowModeWait = false;

        // ============================
        // 時間管理
        // ============================

        /// <summary>
        /// ゲーム内の仮想時刻（昼夜サイクル用）
        /// </summary>
        public static TimeSpan virtualTime;

        /// <summary>
        /// ゲーム実行中フラグ
        /// </summary>
        public static bool isRunning = true;

        /// <summary>
        /// 初回起動フラグ
        /// </summary>
        public static bool isFirstTime = true;

        /// <summary>
        /// ゲーム開始時のカウント
        /// </summary>
        public static int startCount;

        /// <summary>
        /// 前フレームからの経過時間（秒）
        /// </summary>
        public static float loopTime = 0.0f;

        /// <summary>
        /// 前フレームの時刻
        /// </summary>
        public static float lastTime = 0.0f;

        /// <summary>
        /// UTC開始時刻（Ticks）
        /// </summary>
        public static long start_UtcNow_Ticks;

        // ============================
        // システム管理クラス参照
        // ============================

        /// <summary>
        /// 入力管理
        /// </summary>
        public static Input INP;

        /// <summary>
        /// ゲームデータ管理
        /// </summary>
        public static GameData DAT;

        /// <summary>
        /// 操作・制御クラス
        /// </summary>
        public static BaseController CTRL;

        /// <summary>
        /// 描画クラス
        /// </summary>
        public static BaseView VIEW;

        /// <summary>
        /// イベント管理クラス
        /// </summary>
        public static BaseEvents EVNT;

        /// <summary>
        /// ワールド管理クラス
        /// </summary>
        public static World WRLD;

        /// <summary>
        /// 現在操作しているユーザーID
        /// </summary>
        public static int UserID;

        // ============================
        // シーン・ステージ管理
        // ============================

        /// <summary>
        /// ステージ内に滞在中かどうか
        /// </summary>
        public static bool isStayStage = true;

        /// <summary>
        /// 現在のシーンID
        /// </summary>
        public static int SceneID = 0;

        /// <summary>
        /// 現在のステージID
        /// </summary>
        public static int StageID = 0;

        /// <summary>
        /// 現在のイベントID
        /// </summary>
        public static int EventID = 0;

        /// <summary>
        /// 現在のイベントサブID
        /// </summary>
        public static int EventSubID = 0;

        /// <summary>
        /// 次に遷移するシーンID
        /// </summary>
        public static int NextSceneID = 0;

        /// <summary>
        /// 次に遷移するステージID
        /// </summary>
        public static int NextStageID = 0;

        /// <summary>
        /// 次に遷移するイベントID
        /// </summary>
        public static int NextEventID = 0;

        /// <summary>
        /// 次に遷移するイベントサブID
        /// </summary>
        public static int NextEventSubID = 0;

        // ============================
        // ゲーム要素
        // ============================

        /// <summary>
        /// NPC管理クラス
        /// </summary>
        public static NpcManager NPC;

        /// <summary>
        /// 画面歪みシェーダー
        /// </summary>
        public static Distortion ShaderDistortion;

        /// <summary>
        /// エンディングフラグ
        /// </summary>
        public static bool Ending = false;
    }
}
