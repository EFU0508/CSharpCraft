using System;
using System.Runtime.InteropServices;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// 画面全体（または一部）に歪みエフェクトをかけるためのクラス
    /// オフスクリーンに描画した画面を、シェーダで歪ませて再描画する
    /// </summary>
    public class Distortion : IDisposable
    {
        /// <summary>
        /// 歪み強度の最大値
        /// </summary>
        const float DistPowerMax = 3f;

        /// <summary>
        /// 現在の歪み強度（0 = 歪みなし）
        /// </summary>
        public float DistPower = 0.0f;

        /// <summary>
        /// 歪み強度を増減させる方向（-1:弱める / +1:強める）
        /// </summary>
        private float target = 0.0f;

        /// <summary>
        /// 歪み遷移中かどうかのフラグ
        /// </summary>
        private bool targetDist = false;

        /// <summary>
        /// 歪み遷移中かどうかを外部から参照・制御するためのプロパティ
        /// </summary>
        public bool IsTargetDist
        {
            get { return targetDist; }
            set { targetDist = value; }
        }

        /// <summary>
        /// 歪み用バーテックスシェーダ
        /// </summary>
        private int DistVS;

        /// <summary>
        /// 歪み用ピクセルシェーダ
        /// </summary>
        private int DistPS;

        /// <summary>
        /// シェーダ定数バッファ
        /// </summary>
        private int DistCB;

        /// <summary>
        /// 歪みシェーダに渡す定数バッファ構造体
        /// </summary>
        private struct sDistCB
        {
            public float time;          // 時間（アニメーション用）
            public float resX;          // 画面幅
            public float resY;          // 画面高さ
            public float areaMinX;      // 歪み適用領域 X 最小
            public float areaMaxX;      // 歪み適用領域 X 最大
            public float areaMinY;      // 歪み適用領域 Y 最小
            public float areaMaxY;      // 歪み適用領域 Y 最大
            public float distortPower;  // 歪み強度
        }

        /// <summary>
        /// フルスクリーン四角形（2ポリゴン）の頂点配列
        /// </summary>
        private VERTEX3DSHADER[] fsq = new VERTEX3DSHADER[4];

        /// <summary>
        /// フルスクリーン四角形のインデックス
        /// </summary>
        private ushort[] fsqIdx = new ushort[] { 0, 1, 2, 2, 1, 3 };

        /// <summary>
        /// オフスクリーン描画用テクスチャ
        /// </summary>
        private int sceneTex;

        /// <summary>
        /// コンストラクタ
        /// シェーダ・定数バッファ・オフスクリーンを初期化
        /// </summary>
        public Distortion()
        {
            // フルスクリーン四角形の頂点設定
            fsq[0].pos = new VECTOR(0, 0, 0);
            fsq[0].u = 0.0f;
            fsq[0].v = 1.0f;
            fsq[1].pos = new VECTOR(StClass.GAME_WIDTH, 0, 0);
            fsq[1].u = 1.0f;
            fsq[1].v = 1.0f;
            fsq[2].pos = new VECTOR(0, StClass.GAME_HEIGHT, 0);
            fsq[2].u = 0.0f;
            fsq[2].v = 0.0f;
            fsq[3].pos = new VECTOR(StClass.GAME_WIDTH, StClass.GAME_HEIGHT, 0);
            fsq[3].u = 1.0f;
            fsq[3].v = 0.0f;

            // 歪み用シェーダ読み込み
            DistVS = LoadVertexShader(".\\Resources\\Shader\\DistortionVS.vso");
            DistPS = LoadPixelShader(".\\Resources\\Shader\\DistortionPS.pso");

            // シェーダ定数バッファ作成
            DistCB = CreateShaderConstantBuffer(Marshal.SizeOf<sDistCB>());

            // シーン描画用のオフスクリーン作成
            sceneTex = MakeScreen(StClass.GAME_WIDTH, StClass.GAME_HEIGHT, TRUE);

            DistPower = 0.0f;
        }

        /// <summary>
        /// 使用したDxLibリソースの解放
        /// </summary>
        public void Dispose()
        {
            DeleteGraph(sceneTex);
            DeleteShader(DistVS);
            DeleteShader(DistPS);
            DeleteShaderConstantBuffer(DistCB);
        }

        /// <summary>
        /// 歪みを弱める（フェードイン的演出）
        /// </summary>
        public void FadeIn()
        {
            target = -1.0f;
            targetDist = true;
        }

        /// <summary>
        /// 歪みを強める（フェードアウト的演出）
        /// </summary>
        public void FadeOut()
        {
            target = 1.0f;
            targetDist = true;
        }

        /// <summary>
        /// 歪み強度の更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        /// <returns>遷移完了した場合 true</returns>
        public bool Update(float deltaTime)
        {
            // 歪み強度を時間に応じて増減
            DistPower += deltaTime * target;

            // 下限チェック
            if (DistPower < 0f)
            {
                DistPower = 0f;
                targetDist = false;
                return true;
            }
            // 上限チェック
            else if (DistPower > DistPowerMax)
            {
                DistPower = DistPowerMax;
                targetDist = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// シェーダ適用前の描画モード設定
        /// （この後、通常の3D描画を行う）
        /// </summary>
        public void SetShaderMode()
        {
            // オフスクリーンに描画
            SetDrawScreen(sceneTex);

            SetUseZBufferFlag(TRUE);
            SetWriteZBufferFlag(TRUE);
            SetCameraNearFar(0.1f, 1000.0f);
            SetZBufferBitDepth(24);
        }

        /// <summary>
        /// 歪みシェーダを使って画面に描画する
        /// </summary>
        public void SetShaderDraw(float areaMinX, float areaMaxX, float areaMinY, float areaMaxY, float distortPower)
        {
            // 描画先をバックバッファに戻す
            SetDrawScreen(DX_SCREEN_BACK);
            SetDrawBlendMode(DX_BLENDMODE_ALPHA, 255);

            // 定数バッファに値を設定
            sDistCB cb = new sDistCB();
            cb.time = StClass.lastTime;
            cb.resX = StClass.GAME_WIDTH;
            cb.resY = StClass.GAME_HEIGHT;
            cb.areaMinX = areaMinX;
            cb.areaMaxX = areaMaxX;
            cb.areaMinY = areaMinY;
            cb.areaMaxY = areaMaxY;
            cb.distortPower = distortPower;

            // 定数バッファ更新
            IntPtr intPtr = GetBufferShaderConstantBuffer(DistCB);
            Marshal.StructureToPtr(cb, intPtr, false);
            UpdateShaderConstantBuffer(DistCB);
            SetShaderConstantBuffer(DistCB, DX_SHADERTYPE_PIXEL, 4);

            // シェーダ・テクスチャ設定
            SetUseVertexShader(DistVS);
            SetUsePixelShader(DistPS);
            SetUseTextureToShader(0, sceneTex);

            // フルスクリーンポリゴン描画
            DrawPolygonIndexed3DToShader(fsq, fsq.Length, fsqIdx, fsqIdx.Length);

            // シェーダ解除
            SetUseVertexShader(-1);
            SetUsePixelShader(-1);
            SetUseTextureToShader(0, -1);

            SetDrawBlendMode(DX_BLENDMODE_NOBLEND, 0);
        }
    }
}
