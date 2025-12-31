using ModelLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// ゲーム全体で共有するデータ管理クラス
    /// </summary>
    public class GameData : IDisposable
    {
        // ===== モデル関連 =====

        /// <summary>
        /// モデル管理クラス（アニメーション・描画制御）
        /// </summary>
        public Model model;

        /// <summary>
        /// プレイヤーやNPCなどのモデル情報をIDで管理
        /// </summary>
        public Dictionary<int, ModelInfo> modelInfo;

        // ===== ブロック関連 =====

        /// <summary>
        /// ブロックテクスチャのグラフハンドル
        /// </summary>
        public int blockTextures;

        /// <summary>
        /// ホットバー等で使うブロックアイコン
        /// </summary>
        public int[] blockIcons;

        /// <summary>
        /// 現在選択中のブロックID（0は削除）
        /// </summary>
        public ushort selectBlockID;

        /// <summary>
        /// 当たり判定・物理処理クラス
        /// </summary>
        public Physics physics;

        // ===== 環境オブジェクト =====

        /// <summary>
        /// 太陽モデルのハンドル
        /// </summary>
        public int SunHandle;

        /// <summary>
        /// 月モデルのハンドル
        /// </summary>
        public int MoonHandle;

        /// <summary>
        /// 霧用モデル
        /// </summary>
        public int FogHandle;

        // ===== サウンド関連 =====

        /// <summary>
        /// フィールドBGM（複数）
        /// </summary>
        public int[] mp3_Field;
        /// <summary>
        /// 現在再生中のフィールドBGM番号
        /// </summary>
        public int mp3_Field_Number;

        /// <summary>
        /// エンディングBGM
        /// </summary>
        public int mp3_Ending;

        /// <summary>
        /// 建築音
        /// </summary>
        public int mp3_Build;

        /// <summary>
        /// 弓を放つ音
        /// </summary>
        public int mp3_Cob;

        /// <summary>
        /// 死亡・ミス時の効果音
        /// </summary>
        public int mp3_Mortality;

        /// <summary>
        /// ブロック削除音
        /// </summary>
        public int mp3_Scrape;

        /// <summary>
        /// コンストラクタ（初期化処理）
        /// </summary>
        public GameData()
        {
            // フィールドBGMは4曲分確保
            mp3_Field = new int[4];

            // モデル管理クラス生成
            model = new Model();

            // モデル情報Dictionary生成
            modelInfo = new Dictionary<int, ModelInfo>();

            // ===== ゲームループ時間管理 =====
            StClass.isRunning = true;
            StClass.loopTime = 0.0f;
            StClass.startCount = GetNowCount();
            StClass.lastTime = (GetNowCount() - StClass.startCount) / 1000.0f;

            // 初期状態は「削除モード」
            selectBlockID = 0;

            // 物理判定初期化
            physics = new Physics();

            // 画面歪みシェーダ初期化
            StClass.ShaderDistortion = new Distortion();
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public void Dispose()
        {
            // 歪みシェーダ解放
            StClass.ShaderDistortion?.Dispose();
            StClass.ShaderDistortion = null;

            // 物理クラス解放
            physics?.Dispose();
            physics = null;

            // 全モデル情報を解放
            foreach (int i in modelInfo.Keys.ToList())
            {
                modelInfo[i]?.Dispose();
                modelInfo[i] = null;
            }
            // モデル管理クラス解放
            model?.Dispose();
            model = null;

            // 環境モデル解放
            DeleteGraph(SunHandle);
            DeleteGraph(MoonHandle);

            // BGM停止
            StopSoundMem(StClass.DAT.mp3_Field[StClass.DAT.mp3_Field_Number]);
            StopSoundMem(StClass.DAT.mp3_Ending);
        }
    }
}
