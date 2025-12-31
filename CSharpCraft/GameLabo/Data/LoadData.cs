using ModelLib;
using System;
using static CmnDxlib.Func;
using static DX;
using static ModelLib.Model;

namespace GameLabo
{
    /// <summary>
    /// ゲーム起動時のリソース読み込みを段階的に行うクラス
    /// </summary>
    public class LoadData : IDisposable
    {
        /// <summary>
        /// 全ロード完了フラグ
        /// </summary>
        public bool isLoadData;

        /// <summary>
        /// サブロード中フラグ（大量処理用）
        /// </summary>
        public bool isSubLoadData;

        /// <summary>
        /// メインロード進捗カウンタ
        /// </summary>
        private int Counter;

        /// <summary>
        /// サブロード進捗カウンタ
        /// </summary>
        private int SubCounter;

        /// <summary>
        /// サブロードの最大数
        /// </summary>
        private int SubCounterMax;

        /// <summary>
        /// 実行するロード処理を順番に保持する配列
        /// </summary>
        private Action[] Methods;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LoadData()
        {
            isLoadData = false;
            Counter = 0;
            isSubLoadData = false;
            SubCounter = 0;
            SubCounterMax = 0;

            // ロード処理を順番に登録
            Methods = new Action[]
            {
                mp3_Field,
                mp3_Ending,
                mp3_Build,
                mp3_Cob,
                mp3_Mortality,
                mp3_Scrape,
                BlockChipData,
                RefMeshData,
                SunHandle,
                MoonHandle,
                FogHandle,
                slime_kit,
                slimebes_kit,
                capybara_kit,
                blockIcons,
                wooden_bow,
                arrow_stick,
                usochan,
            };
        }

        public void Dispose()
        {
            // 解放処理
        }

        /// <summary>
        /// 毎フレーム1つずつロード処理を進める
        /// </summary>
        public void Update()
        {
            // 現在のロード処理を実行
            Methods[Counter]();

            // サブロード中でなければ次へ
            if (!isSubLoadData)
            {
                Counter++;
            }

            // 全て実行し終えたらロード完了
            if (Counter >= Methods.Length)
            {
                isLoadData = true;
            }
        }

        /// <summary>
        /// ロード画面描画
        /// </summary>
        public void Draw()
        {
            ClearDrawScreen();

            SetFontSize(16);
            int left = StClass.GAME_WIDTH / 2;
            int top = StClass.GAME_HEIGHT / 2;

            // メインロード進捗表示
            DrawString(left - 64, StClass.GAME_HEIGHT - 40, $"Now loading {(int)((float)Counter / (float)Methods.Length * 100.0f)}%", GetColor(255, 255, 255));

            // サブロード進捗表示
            if (isSubLoadData)
            {
                DrawString(left - 64, StClass.GAME_HEIGHT - 20, $"Now sub loading {(int)((float)SubCounter / (float)SubCounterMax * 100.0f)}%", GetColor(255, 255, 255));
            }
        }

        /// <summary>
        /// フィールドBGM読み込み
        /// </summary>
        private void mp3_Field()
        {
            StClass.DAT.mp3_Field[0] = LoadSoundMem(".\\Resources\\BGM\\Field1.mp3");
            StClass.DAT.mp3_Field[1] = LoadSoundMem(".\\Resources\\BGM\\Field2.mp3");
            StClass.DAT.mp3_Field[2] = LoadSoundMem(".\\Resources\\BGM\\Field3.mp3");
            StClass.DAT.mp3_Field[3] = LoadSoundMem(".\\Resources\\BGM\\Field4.mp3");
            // ランダムに再生曲を選択
            StClass.DAT.mp3_Field_Number = GetRand(StClass.DAT.mp3_Field.Length - 1);
            ChangeVolumeSoundMem(120, StClass.DAT.mp3_Field[StClass.DAT.mp3_Field_Number]);
        }

        /// <summary>
        /// エンディングBGM読み込み
        /// </summary>
        private void mp3_Ending()
        {
            StClass.DAT.mp3_Ending = LoadSoundMem(".\\Resources\\BGM\\Ending.mp3");
            ChangeVolumeSoundMem(128, StClass.DAT.mp3_Ending);
        }

        /// <summary>
        /// 建築SE読み込み
        /// </summary>
        private void mp3_Build()
        {
            StClass.DAT.mp3_Build = LoadSoundMem(".\\Resources\\BGM\\Build.mp3");
            ChangeVolumeSoundMem(128, StClass.DAT.mp3_Build);
        }

        /// <summary>
        /// 弓発射SE読み込み
        /// </summary>
        private void mp3_Cob()
        {
            StClass.DAT.mp3_Cob = LoadSoundMem(".\\Resources\\BGM\\Cob.mp3");
            ChangeVolumeSoundMem(128, StClass.DAT.mp3_Cob);
        }

        /// <summary>
        /// 死亡SE読み込み
        /// </summary>
        private void mp3_Mortality()
        {
            StClass.DAT.mp3_Mortality = LoadSoundMem(".\\Resources\\BGM\\Mortality.mp3");
            ChangeVolumeSoundMem(128, StClass.DAT.mp3_Mortality);
        }

        /// <summary>
        /// ブロック削除SE読み込み
        /// </summary>
        private void mp3_Scrape()
        {
            StClass.DAT.mp3_Scrape = LoadSoundMem(".\\Resources\\BGM\\Scrape.mp3");
            ChangeVolumeSoundMem(128, StClass.DAT.mp3_Scrape);
        }

        /// <summary>
        /// 木製の弓モデル読み込み
        /// </summary>
        private void wooden_bow()
        {
            StClass.DAT.model.Bow_WoodHandle = MV1LoadModel(".\\Resources\\HuntingKit\\Wooden_Bow.mv1");
            MV1SetScale(StClass.DAT.model.Bow_WoodHandle, VGet(0.01f, 0.01f, 0.01f));
            MV1SetPosition(StClass.DAT.model.Bow_WoodHandle, VGet(0f, 0f, 0f));
            MV1SetRotationXYZDegree(StClass.DAT.model.Bow_WoodHandle, VGet(0f, 0f, 0f));
        }

        /// <summary>
        /// 矢モデル読み込み
        /// </summary>
        private void arrow_stick()
        {
            StClass.DAT.model.Arrow_WoodHandle = MV1LoadModel(".\\Resources\\HuntingKit\\Arrow_stick.mv1");
        }

        /// <summary>
        /// プレイヤーキャラ「うそちゃん」初期化
        /// </summary>
        private void usochan()
        {
            // モデルデータ読み込み
            StClass.DAT.model.ModelBaseRead((int)Model_CharaType.Usochan, ".\\Resources\\Usochan\\usochan_base.mv1");
            StClass.DAT.model.ModelKitRead((int)Model_CharaType.Usochan, ".\\Resources\\Usochan\\usochan_kit.mv1");
            StClass.DAT.model.ModelUpperRead((int)Model_CharaType.Usochan, ".\\Resources\\Usochan\\usochan_base_upper.mv1");

            // モデル情報初期化
            ModelInfo m = new ModelInfo();
            m.Handle = -1;
            m.Scale = VGet(0.01f, 0.01f, 0.01f);
            m.Rotation = VGet(0, 180, 0);
            m.Position = VGet(0.5f, 128f, 0.5f);
            m.ModelType = (int)Model_CharaType.Usochan;
            m.DressUpTop = -1;
            m.DressUpUpper = 0;
            m.DressUpLower = -1;
            m.AnimeIndex = 0;
            m.UserName = "うそちゃん";

            // モデル複製と装備設定
            m.DuplicateModel(
                StClass.DAT.model.BaseModel[m.ModelType].Handle,
                m.ModelType,
                StClass.DAT.model.Arrow_WoodHandle,
                StClass.DAT.model.FishingHookHandle);

            // 変形・配置反映
            StClass.DAT.model.SetScale(ref m, m.Scale, StClass.DAT.model.BaseModel[m.ModelType]);
            StClass.DAT.model.SetRotationXYZDegree(m);
            StClass.DAT.model.SetPosition(m);

            // 矢・釣り針調整
            MV1SetScale(m.ArrowHandle, VGet(0.01f, 0.01f, 0.01f));
            MV1SetRotationXYZDegree(m.ArrowHandle, VGet(0f, 0f, 0f));
            MV1SetScale(m.HookHandle, VGet(0.001f, 0.001f, 0.001f));
            MV1SetRotationXYZDegree(m.HookHandle, VGet(0f, 0f, 0f));

            // プレイヤーとして登録
            StClass.DAT.modelInfo[StClass.UserID] = m;
        }

        /// <summary>
        /// ブロックテクスチャ読み込み
        /// </summary>
        private void BlockChipData()
        {
            StClass.DAT.blockTextures = LoadGraph(".\\Resources\\Various\\BlockChip.png");
        }

        /// <summary>
        /// ブロック描画用マテリアル設定
        /// </summary>
        private void RefMeshData()
        {
            VECTOR TranslateVector = new VECTOR(0f, 0f, 0f);
            MATRIX TranslateMatrix = MGetTranslate(TranslateVector);
            SetTransformToWorld(TranslateMatrix);

            MATERIALPARAM blockMaterial;
            blockMaterial.Diffuse = GetColorF(255, 255, 255, 255);
            blockMaterial.Specular = GetColorF(255, 255, 255, 255);
            blockMaterial.Ambient = GetColorF(0.0f, 0.0f, 0.0f, 0.0f);
            blockMaterial.Emissive = GetColorF(0.5f, 0.5f, 0.5f, 0.5f);
            blockMaterial.Power = 0f;
            SetMaterialParam(blockMaterial);
        }

        /// <summary>
        /// 太陽テクスチャ読み込み
        /// </summary>
        private void SunHandle()
        {
            StClass.DAT.SunHandle = LoadGraph(".\\Resources\\Various\\sun.png");
        }

        /// <summary>
        /// 月テクスチャ読み込み
        /// </summary>
        private void MoonHandle()
        {
            StClass.DAT.MoonHandle = LoadGraph(".\\Resources\\Various\\moon.png");
        }

        /// <summary>
        /// 霧テクスチャ読み込み
        /// </summary>
        private void FogHandle()
        {
            StClass.DAT.FogHandle = LoadGraph(".\\Resources\\Various\\Fog.png");
        }

        /// <summary>
        /// スライムモデル読み込み
        /// </summary>
        private void slime_kit()
        {
            StClass.DAT.model.ModelBaseRead((int)Model_CharaType.Slime, ".\\Resources\\Slime\\Slime_base.mv1");
        }

        /// <summary>
        /// スライムベスモデル読み込み
        /// </summary>
        private void slimebes_kit()
        {
            StClass.DAT.model.ModelBaseRead((int)Model_CharaType.SlimeBes, ".\\Resources\\SlimeBes\\SlimeBes_base.mv1");
        }

        /// <summary>
        /// カピバラモデル読み込み
        /// </summary>
        private void capybara_kit()
        {
            StClass.DAT.model.ModelBaseRead((int)Model_CharaType.Capybara, ".\\Resources\\Capybara\\capybara.mv1");
        }

        /// <summary>
        /// ブロックアイコン分割読み込み
        /// </summary>
        private void blockIcons()
        {
            const int BLOCK_COUNT = 11;

            StClass.DAT.blockIcons = new int[BLOCK_COUNT];
            LoadDivGraph(
                    ".\\Resources\\Various\\BlockChip.png",
                    BLOCK_COUNT,
                    1,
                    BLOCK_COUNT,
                    16,
                    16,
                    StClass.DAT.blockIcons
                );
        }
    }
}
