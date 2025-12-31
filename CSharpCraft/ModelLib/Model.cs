using System;
using System.Collections.Generic;
using System.Diagnostics;
using static CmnDxlib.Func;
using static DX;

namespace ModelLib
{
    /// <summary>
    /// キャラクターモデル管理クラス
    /// ・ベースモデル
    /// ・着せ替え（上・下・帽子）
    /// ・アニメーション再生
    /// ・描画
    /// を一括管理する
    /// </summary>
    public class Model : IDisposable
    {
        // アニメーション番号定義
        public const int C001_Idle = 0;
        public const int C002_Walk = 1;
        public const int C003_Run = 2;
        public const int C004_Jump = 3;
        public const int C005_Fishing_Idle = 4;
        public const int C006_Fishing_Casting = 5;
        public const int C007_Fishing_Hit = 6;
        public const int C008_Aim_Idle = 7;
        public const int C009_Aim_Recoil = 8;
        public const int C010_Walk_Forward = 9;
        public const int C011_Walk_Back = 10;
        public const int C012_Walk_Left = 11;
        public const int C013_Walk_Right = 12;
        public const int C014_TorchIdle = 13;
        public const int C015_Ouch = 14;
        public const int C016_TorchIdleRight = 15;
        public const int C017_Jumping = 16;
        public const int C018_Gangnam_Style = 17;
        public const int C019_Sit = 18;

        // フレーム名定数（モデル内部のボーン名）
        private const string strRightHand = "RightHand";
        private const string strLeftHand = "LeftHand";
        private const string strRightHandMiddle1 = "RightHandMiddle1";
        private const string strLeftHandMiddle1 = "LeftHandMiddle1";
        private const string strHeadTop_End_end = "HeadTop_End_end";

        // 装備・小物用ハンドル

        // 釣り関連
        public int FishingRodHandle;
        public int FishingRod_end;
        public int FishingHookHandle;
        public int HitHandle;

        // アーチャー関連
        public int Bow_WoodHandle;
        public int Arrow_WoodHandle;

        /// <summary>
        /// キャラクター種別
        /// </summary>
        public enum Model_CharaType : int
        {
            Usochan = 6,
            Slime = 7,
            SlimeBes = 8,
            Capybara = 12,
        }

        // モデル構造体定義

        /// <summary>
        /// ベースモデル情報
        /// </summary>
        public struct BaseInfo
        {
            public int Handle;              // モデルハンドル
            public VECTOR MminV;            // モデル最小座標
            public VECTOR MmaxV;            // モデル最大座標
            public int FrameNum;            // フレーム数

            // 各重要ボーンのフレーム番号
            public int HeadTop_End_end;
            public int RightHand;
            public int LeftHand;
            public int RightHandMiddle1;
            public int LeftHandMiddle1;
        }

        // 帽子・頭装備
        public struct TopInfo
        {
            public int Handle;
        }

        // 上半身装備
        public struct UpperInfo
        {
            public int Handle;
            public int FaceNo;  // 顔テクスチャ番号
            public int BodyNo;  // ボディマテリアル番号
        }

        // 下半身装備
        public struct LowerInfo
        {
            public int Handle;
            public int BodyNo;
        }

        // モデル管理コンテナ
        public Dictionary<int, BaseInfo> BaseModel;
        public Dictionary<int, List<TopInfo>> TopModel;
        public Dictionary<int, List<UpperInfo>> UpperModel;
        public Dictionary<int, List<LowerInfo>> LowerModel;
        public Dictionary<int, List<int>> FaceList;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Model()
        {
            BaseModel = new Dictionary<int, BaseInfo>();
            BaseModel.Clear();
            TopModel = new Dictionary<int, List<TopInfo>>();
            TopModel.Clear();
            UpperModel = new Dictionary<int, List<UpperInfo>>();
            UpperModel.Clear();
            LowerModel = new Dictionary<int, List<LowerInfo>>();
            LowerModel.Clear();
            FaceList = new Dictionary<int, List<int>>();
            FaceList.Clear();
        }

        /// <summary>
        /// 全モデルリソースの解放
        /// </summary>
        public void Dispose()
        {
            foreach (KeyValuePair<int, BaseInfo> m in BaseModel)
            {
                MV1DeleteModel(m.Value.Handle);
            }

            foreach (KeyValuePair<int, List<TopInfo>> m in TopModel)
            {
                foreach (TopInfo ks in m.Value)
                {
                    MV1DeleteModel(ks.Handle);
                }
            }

            foreach (KeyValuePair<int, List<UpperInfo>> m in UpperModel)
            {
                foreach (UpperInfo ks in m.Value)
                {
                    MV1DeleteModel(ks.Handle);
                }
            }

            foreach (KeyValuePair<int, List<LowerInfo>> m in LowerModel)
            {
                foreach (LowerInfo ks in m.Value)
                {
                    MV1DeleteModel(ks.Handle);
                }
            }

            foreach (KeyValuePair<int, List<int>> f in FaceList)
            {
                foreach (int i in f.Value)
                {
                    DeleteGraph(i);
                }
            }
        }

        /// <summary>
        /// モデルから「face」マテリアルのテクスチャ番号を取得
        /// </summary>
        public int GetFaceNo(int Handle)
        {
            int FaceNo = -1;

            {
                int matNum = DX.MV1GetMaterialNum(Handle);

                for (int i = 0; i < matNum; i++)
                {
                    int texId = DX.MV1GetMaterialDifMapTexture(Handle, i);
                    string n = MV1GetMaterialName(Handle, i);
                    if (n == "face")
                    {
                        FaceNo = texId;
                        break;
                    }
                }
            }

            return FaceNo;
        }

        /// <summary>
        /// モデルから「body」マテリアル番号を取得
        /// </summary>
        public int GetBodyNo(int Handle)
        {
            int BodyNo = -1;

            {
                int matNum = DX.MV1GetMaterialNum(Handle);

                for (int i = 0; i < matNum; i++)
                {
                    int texId = DX.MV1GetMaterialDifMapTexture(Handle, i);
                    string n = MV1GetMaterialName(Handle, i);
                    if (n == "body")
                    {
                        BodyNo = i;
                        break;
                    }
                }
            }

            return BodyNo;
        }

        /// <summary>
        /// ベースモデルの読み込み
        /// </summary>
        public void ModelBaseRead(int ModelNo, string FileName)
        {
            int handle = MV1LoadModel(FileName);

            // 初期姿勢
            MV1SetScale(handle, VGet(1f, 1f, 1f));
            MV1SetPosition(handle, VGet(0f, 0f, 0f));
            MV1SetRotationXYZDegree(handle, VGet(0f, 0f, 0f));

            BaseInfo k = new BaseInfo();
            k.Handle = handle;
            // モデルサイズ取得
            MV1GetModelMinMaxPosition(handle, out k.MminV, out k.MmaxV);
            k.FrameNum = MV1GetFrameNum(handle);

            // ボーン探索
            for (int i = 0; i < k.FrameNum; i++)
            {
                string s = MV1GetFrameName(handle, i);
                int j = s.IndexOf(':');
                if (j >= 0)
                {
                    s = s.Substring(j + 1);
                }
                if (s == strHeadTop_End_end)
                {

                    k.HeadTop_End_end = i;
                }
                if (s == strRightHand)
                {

                    k.RightHand = i;
                }
                if (s == strLeftHand)
                {

                    k.LeftHand = i;
                }
                if (s == strRightHandMiddle1)
                {

                    k.RightHandMiddle1 = i;
                }
                if (s == strLeftHandMiddle1)
                {

                    k.LeftHandMiddle1 = i;
                }
                VECTOR Position = MV1GetFramePosition(handle, i);
            }

            BaseModel[ModelNo] = k;
        }

        /// <summary>
        /// モデルキット（着せ替えベース）の初期化
        /// </summary>
        public void ModelKitRead(int ModelNo, string FileName)
        {
            BaseInfo k = BaseModel[ModelNo];
            k.Handle = MV1LoadModel(FileName);
            BaseModel[ModelNo] = k;
            List<TopInfo> TopModelList = new List<TopInfo>();
            TopModel[ModelNo] = TopModelList;
            List<UpperInfo> UpperModelList = new List<UpperInfo>();
            UpperModel[ModelNo] = UpperModelList;
            List<LowerInfo> LowerModelList = new List<LowerInfo>();
            LowerModel[ModelNo] = LowerModelList;
            FaceList[ModelNo] = new List<int>();

        }

        /// <summary>
        /// 上半身装備モデル読み込み
        /// </summary>
        public void ModelUpperRead(int ModelNo, string FileName)
        {
            List<UpperInfo> UpperModelList = UpperModel[ModelNo];
            UpperInfo ks = new UpperInfo();
            ks.Handle = MV1LoadModel(FileName);
            ks.FaceNo = GetFaceNo(ks.Handle);
            ks.BodyNo = GetBodyNo(ks.Handle);
            UpperModelList.Add(ks);
            UpperModel[ModelNo] = UpperModelList;
        }

        /// <summary>
        /// モデルスケール設定＋当たり判定用パラメータ算出
        /// </summary>
        public void SetScale(ref ModelInfo info, VECTOR scale, BaseInfo bInfo)
        {
            MV1SetScale(info.Handle, scale);

            BaseInfo baseInfo = BaseModel[info.ModelType];
            info.Height = (baseInfo.MmaxV.y - baseInfo.MminV.y) / 2f;
            info.Width = (baseInfo.MmaxV.x - baseInfo.MminV.x);

            // カプセル判定サイズ計算
            info.CapsuleR = (baseInfo.MmaxV.x - baseInfo.MminV.x) / 2f;
            info.CapsuleH = ((baseInfo.MmaxV.y - baseInfo.MminV.y) / 2f) - info.CapsuleR;
            info.CapsuleC = VSub(VGet(0f, 0f, 0f), VDiv(VAdd(baseInfo.MminV, baseInfo.MmaxV), VGet(2f, 2f, 2f)));

            info.MminV = VMul(baseInfo.MminV, scale);
            info.MmaxV = VMul(baseInfo.MmaxV, scale);
            info.Height = info.Height * scale.y;
            info.Width = info.Width * scale.x;
            info.CapsuleR = ((bInfo.MmaxV.x - bInfo.MminV.x) / 2f) * scale.x;
            info.CapsuleH = info.CapsuleH * scale.y;
            info.CapsuleC = VMul(info.CapsuleC, scale);

            BaseModel[info.ModelType] = baseInfo;
        }

        /// <summary>
        /// モデル回転設定（度数）
        /// </summary>
        public void SetRotationXYZDegree(ModelInfo info)
        {
            MV1SetRotationXYZDegree(info.Handle, info.Rotation);
        }

        /// <summary>
        /// モデル位置設定
        /// </summary>
        public void SetPosition(ModelInfo info)
        {
            MV1SetPosition(info.Handle, info.Position);
        }

        public void SetPosition(ModelInfo info, VECTOR position)
        {
            MV1SetPosition(info.Handle, position);
        }

        /// <summary>
        /// モデル描画（着せ替え込み）
        /// </summary>
        public void Draw(ModelInfo info, VECTOR adjust)
        {
            SetPosition(info, VAdd(info.Position, adjust));
            Draw(info);
        }

        public void Draw(ModelInfo info)
        {
            if ((info.Handle < 0) || (info.ModelType < 0))
            {
                return;
            }

            // ベース描画
            MV1DrawModel(info.Handle);

            // フレーム同期（着せ替え用）
            for (int i = 0; i < BaseModel[info.ModelType].FrameNum; i++)
            {
                MATRIX m = MV1GetFrameLocalWorldMatrix(info.Handle, i);
                MV1SetFrameUserLocalWorldMatrix(UpperModel[info.ModelType][info.DressUpUpper].Handle, i, m);
                if (info.DressUpLower >= 0)
                {
                    MV1SetFrameUserLocalWorldMatrix(LowerModel[info.ModelType][info.DressUpLower].Handle, i, m);
                }
            }

            // 顔差し替え
            if (info.FaceIndex >= 0)
            {
                MV1SetTextureGraphHandle(
                    UpperModel[info.ModelType][info.DressUpUpper].Handle,
                    UpperModel[info.ModelType][info.DressUpUpper].FaceNo,
                    FaceList[info.ModelType][info.FaceIndex], FALSE);
            }
            MV1DrawModel(UpperModel[info.ModelType][info.DressUpUpper].Handle);

            if (info.DressUpLower >= 0)
            {
                MV1DrawModel(LowerModel[info.ModelType][info.DressUpLower].Handle);
            }

            if (info.DressUpTop >= 0)
            {
                MV1SetScale(TopModel[info.ModelType][info.DressUpTop].Handle, info.Scale);
                VECTOR vEnd = MV1GetFramePosition(info.Handle, BaseModel[info.ModelType].HeadTop_End_end);
                MV1SetPosition(TopModel[info.ModelType][info.DressUpTop].Handle, vEnd);
                MATRIX mat = MV1GetFrameLocalWorldMatrix(info.Handle, BaseModel[info.ModelType].HeadTop_End_end);
                VECTOR rot;
                GetMatrixXYZRotation(mat, out rot.x, out rot.y, out rot.z);
                MV1SetRotationXYZ(TopModel[info.ModelType][info.DressUpTop].Handle, rot);
                MV1DrawModel(TopModel[info.ModelType][info.DressUpTop].Handle);
            }
        }

        public void DrawBase(ModelInfo info, VECTOR adjust)
        {
            SetPosition(info, VAdd(info.Position, adjust));
            DrawBase(info);
        }

        public void DrawBase(ModelInfo info)
        {
            if ((info.Handle < 0) || (info.ModelType < 0))
            {
                return;
            }
            MV1DrawModel(info.Handle);
        }

        /// <summary>
        /// アニメーション再生制御
        /// </summary>
        public bool PlayAnimeModel(ref ModelInfo m, float loopTime, bool Repeat = true, float PlayTime = 0.0f)
        {
            bool move = true;

            // アニメ切り替え時
            if (m.OldAnimeIndex != m.AnimeIndex)
            {
                m.OldAnimeIndex = m.AnimeIndex;
                MV1DetachAnim(m.Handle, m.AttachIndex);
                m.AttachIndex = MV1AttachAnim(m.Handle, m.AnimeIndex);
                m.TotalTime = MV1GetAttachAnimTotalTime(m.Handle, m.AttachIndex);

                m.PlayTime = PlayTime;
                m.Repeat = Repeat;
            }
            else
            {
                m.PlayTime += loopTime * 30f;
            }

            // 再生終了判定
            if (m.PlayTime >= m.TotalTime)
            {
                if (m.Repeat)
                {
                    m.PlayTime = 0.0f;
                }
                else
                {
                    m.PlayTime = m.TotalTime;
                    move = false;
                }

            }

            MV1SetAttachAnimTime(m.Handle, m.AttachIndex, m.PlayTime);

            // 初期フレームで物理リセット
            if (m.PlayTime == 0.0f)
            {
                MV1PhysicsResetState(m.Handle);
            }

            return move;
        }
    }
}
