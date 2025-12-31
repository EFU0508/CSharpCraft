using System;
using System.Collections.Generic;
using static CmnDxlib.Func;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// ボクセルワールド用のチャンク管理クラス
    /// ・チャンク単位でブロック情報を保持
    /// ・見える面だけを頂点化して VertexBuffer を生成
    /// </summary>
    public class Chunks : IDisposable
    {
        // --- ブロックID定義 ---

        // 各ブロックのID定義
        public const ushort Block_Unused = 0;
        public const ushort Block_GoodSoil = 1;
        public const ushort Block_Stone = 2;
        public const ushort Block_Water = 3;
        public const ushort Block_BadSoil = 4;
        public const ushort Block_RedBrick = 5;
        public const ushort Block_Brick = 6;
        public const ushort Block_WeakBrick = 7;
        public const ushort Block_Leaf = 8;
        public const ushort Block_Wood = 9;
        public const ushort Block_Grass = 10;
        public const ushort Block_Obs = 11;

        // 空気ブロック（存在しない扱い）
        public const ushort Block_Air = ushort.MaxValue;

        // --- チャンクサイズ定義 ---

        // 1チャンク内のブロック数
        public static readonly int blockX = 16;
        public static readonly int blockY = 256;
        public static readonly int blockZ = 16;

        // チャンク内の総ブロック数
        public static readonly int blockSize = blockX * blockY * blockZ;

        // --- テクスチャアトラス設定 ---

        // テクスチャアトラスの横タイル数
        const int AtlasTilesX = 6;

        // 縦タイル数（ブロックIDと対応）
        public int AtlasTilesY = 5;

        // 1タイルの横幅（UV）
        const float TileW = 1.0f / AtlasTilesX;

        /// <summary>
        /// チャンク1個分のデータ
        /// </summary>
        public struct Chunk
        {
            // このチャンク用の頂点バッファ
            public int vertexBufferHandle;

            // ブロックID配列
            public ushort[,,] blockIds;
        }

        /// <summary>
        /// チャンク座標 → チャンクデータ
        /// </summary>
        public Dictionary<(int, int, int), Chunk> dicChunks;

        // 全チャンク共通のインデックスバッファ
        public int indexBufferHandle;

        public Chunks()
        {
            dicChunks = new Dictionary<(int, int, int), Chunk>();
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public void Dispose()
        {
            foreach (var chunk in dicChunks.Values)
            {
                DeleteVertexBuffer(chunk.vertexBufferHandle);
            }
            dicChunks.Clear();
            if (indexBufferHandle != 0)
            {
                DeleteIndexBuffer(indexBufferHandle);
                indexBufferHandle = 0;
            }
        }

        // --- 面定義 ---

        /// <summary>
        /// ブロックの6面定義
        /// </summary>
        enum Face
        {
            Top = 0,
            North = 1,
            West = 2,
            South = 3,
            East = 4,
            Bottom = 5
        }

        // --- 可視判定 ---

        /// <summary>
        /// 指定ブロックの隣が「見えるか」を判定
        /// ・チャンク内なら配列参照
        /// ・チャンク外ならワールドから取得
        /// </summary>
        private bool IsVisibleLocal(
            ushort[,,] blocks,
            int x, int y, int z,
            int dx, int dy, int dz,
            int chunkX, int chunkY, int chunkZ)
        {
            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            // チャンク内判定
            if (nx >= 0 && nx < blockX &&
                ny >= 0 && ny < blockY &&
                nz >= 0 && nz < blockZ)
            {
                ushort id = blocks[nx, ny, nz];
                return id == Block_Air ||
                       id == Block_Water ||
                       id == Block_Grass ||
                       id == Block_Obs;
            }

            // チャンク外 → ワールド座標に変換
            int wx = chunkX * blockX + nx;
            int wy = chunkY * blockY + ny;
            int wz = chunkZ * blockZ + nz;

            ushort wid = StClass.WRLD.GetWorldBlockId((wx, wy, wz));

            return wid == Block_Air ||
                   wid == Block_Water ||
                   wid == Block_Grass ||
                   wid == Block_Obs;
        }

        // --- UV計算 ---

        /// <summary>
        /// ブロックIDと面からテクスチャUVを取得
        /// </summary>
        private (float u0, float v0, float u1, float v1) GetTileUV(int blockId, Face face)
        {
            int tileX = (int)face;
            int tileY = blockId;

            float u0 = tileX * TileW;
            float v0 = tileY * 0.0625f;
            float u1 = u0 + TileW;
            float v1 = v0 + 0.0625f;

            // DxLib向けに上下反転
            return (u1, v0, u0, v1);
        }

        // --- 頂点生成 ---

        /// <summary>
        /// 頂点1つを生成
        /// </summary>
        private VERTEX3D MakeVertex(float x, float y, float z, float u, float v, VECTOR normal)
        {
            VERTEX3D vert = new VERTEX3D();
            vert.pos = VGet(x, y, z);
            vert.dif = GetColorU8(255, 255, 255, 255);
            vert.norm = normal;
            vert.u = u;
            vert.v = v;
            return vert;
        }

        /// <summary>
        /// 指定面のポリゴン（2三角形）を追加
        /// </summary>
        private void AddFace(List<VERTEX3D> list, int segX, int segY, int segZ,
                             Face face, int blockId)
        {
            var (u0, v0, u1, v1) = GetTileUV(blockId, face);

            VERTEX3D v1_, v2_, v3_, v4_;
            VECTOR normal = VGet(0, 1, 0);

            // 面ごとの頂点配置と法線
            switch (face)
            {
                case Face.Top:
                    normal = VGet(0, 1, 0);
                    v1_ = MakeVertex(segX, segY + 1, segZ, u0, v1, normal);
                    v2_ = MakeVertex(segX, segY + 1, segZ + 1, u0, v0, normal);
                    v3_ = MakeVertex(segX + 1, segY + 1, segZ + 1, u1, v0, normal);
                    v4_ = MakeVertex(segX + 1, segY + 1, segZ, u1, v1, normal);
                    break;

                case Face.Bottom:
                    normal = VGet(0, -1, 0);
                    v1_ = MakeVertex(segX, segY, segZ, u0, v1, normal);
                    v2_ = MakeVertex(segX + 1, segY, segZ, u1, v1, normal);
                    v3_ = MakeVertex(segX + 1, segY, segZ + 1, u1, v0, normal);
                    v4_ = MakeVertex(segX, segY, segZ + 1, u0, v0, normal);
                    break;

                case Face.North:
                    normal = VGet(0, 0, -1);
                    v1_ = MakeVertex(segX, segY, segZ, u1, v1, normal);
                    v2_ = MakeVertex(segX, segY + 1, segZ, u1, v0, normal);
                    v3_ = MakeVertex(segX + 1, segY + 1, segZ, u0, v0, normal);
                    v4_ = MakeVertex(segX + 1, segY, segZ, u0, v1, normal);
                    break;

                case Face.South:
                    normal = VGet(0, 0, 1);
                    v1_ = MakeVertex(segX, segY, segZ + 1, u0, v1, normal);
                    v2_ = MakeVertex(segX + 1, segY, segZ + 1, u1, v1, normal);
                    v3_ = MakeVertex(segX + 1, segY + 1, segZ + 1, u1, v0, normal);
                    v4_ = MakeVertex(segX, segY + 1, segZ + 1, u0, v0, normal);
                    break;

                case Face.West:
                    normal = VGet(-1, 0, 0);
                    v1_ = MakeVertex(segX, segY, segZ, u0, v1, normal);
                    v2_ = MakeVertex(segX, segY, segZ + 1, u1, v1, normal);
                    v3_ = MakeVertex(segX, segY + 1, segZ + 1, u1, v0, normal);
                    v4_ = MakeVertex(segX, segY + 1, segZ, u0, v0, normal);
                    break;

                case Face.East:
                    normal = VGet(1, 0, 0);
                    v1_ = MakeVertex(segX + 1, segY, segZ, u1, v1, normal);
                    v2_ = MakeVertex(segX + 1, segY + 1, segZ, u1, v0, normal);
                    v3_ = MakeVertex(segX + 1, segY + 1, segZ + 1, u0, v0, normal);
                    v4_ = MakeVertex(segX + 1, segY, segZ + 1, u0, v1, normal);
                    break;

                default:
                    return;
            }

            // 半透明ブロック処理（水・ガラス等）
            if ((blockId == Chunks.Block_Water) || (blockId == Chunks.Block_Obs))
            {
                v1_.dif.a = 127;
                v2_.dif.a = 127;
                v3_.dif.a = 127;
                v4_.dif.a = 127;
            }

            // 2三角形として登録
            list.Add(v1_);
            list.Add(v2_);
            list.Add(v3_);
            list.Add(v1_);
            list.Add(v3_);
            list.Add(v4_);
        }

        // --- 頂点生成処理 ---

        /// <summary>
        /// 指定チャンクの頂点バッファを生成
        /// </summary>
        public void CreateVertex((int, int, int) pos)
        {
            // チャンク未生成ならブロックIDをワールドから取得
            if (!dicChunks.TryGetValue(pos, out Chunk chunk))
            {
                chunk = new Chunk
                {
                    blockIds = new ushort[blockX, blockY, blockZ]
                };

                int baseX = pos.Item1 * blockX;
                int baseY = pos.Item2 * blockY;
                int baseZ = pos.Item3 * blockZ;

                for (int z = 0; z < blockZ; z++)
                    for (int y = 0; y < blockY; y++)
                        for (int x = 0; x < blockX; x++)
                            chunk.blockIds[x, y, z] =
                                StClass.WRLD.GetWorldBlockId((baseX + x, baseY + y, baseZ + z));
            }

            ushort[,,] blocks = chunk.blockIds;

            var vertices = new List<VERTEX3D>(blockX * blockY * blockZ * 6);

            int offsetX = pos.Item1 * blockX;
            int offsetY = pos.Item2 * blockY;
            int offsetZ = pos.Item3 * blockZ;

            var cx = pos.Item1;
            var cy = pos.Item2;
            var cz = pos.Item3;

            // 各ブロックについて「見える面」だけを生成
            for (int z = 0; z < blockZ; z++)
            {
                for (int y = 0; y < blockY; y++)
                {
                    for (int x = 0; x < blockX; x++)
                    {
                        ushort blockId = blocks[x, y, z];
                        if (blockId == Block_Air)
                            continue;

                        int wx = offsetX + x;
                        int wy = offsetY + y;
                        int wz = offsetZ + z;

                        if (IsVisibleLocal(blocks, x, y, z, 0, 1, 0, cx, cy, cz))
                            AddFace(vertices, wx, wy, wz, Face.Top, blockId);

                        if (IsVisibleLocal(blocks, x, y, z, 0, -1, 0, cx, cy, cz))
                            AddFace(vertices, wx, wy, wz, Face.Bottom, blockId);

                        if (IsVisibleLocal(blocks, x, y, z, 0, 0, -1, cx, cy, cz))
                            AddFace(vertices, wx, wy, wz, Face.North, blockId);

                        if (IsVisibleLocal(blocks, x, y, z, 0, 0, 1, cx, cy, cz))
                            AddFace(vertices, wx, wy, wz, Face.South, blockId);

                        if (IsVisibleLocal(blocks, x, y, z, -1, 0, 0, cx, cy, cz))
                            AddFace(vertices, wx, wy, wz, Face.West, blockId);

                        if (IsVisibleLocal(blocks, x, y, z, 1, 0, 0, cx, cy, cz))
                            AddFace(vertices, wx, wy, wz, Face.East, blockId);
                    }
                }
            }

            // 既存VB削除
            if (chunk.vertexBufferHandle != 0)
            {
                DeleteVertexBuffer(chunk.vertexBufferHandle);
            }

            chunk.vertexBufferHandle =
                CreateVertexBuffer(vertices.Count, DX_VERTEX_TYPE_NORMAL_3D);

            byte[] byteVertex =
                CmnDxlib.Func.ConvertVertex3DToByteArray(vertices.ToArray());

            SetVertexBufferData(0, byteVertex, vertices.Count, chunk.vertexBufferHandle);

            dicChunks[pos] = chunk;
        }

        // --- インデックス生成 ---

        /// <summary>
        /// 全ブロック共通のインデックスバッファを生成
        /// </summary>
        public void CreateIndex()
        {
            int IndexNum = 12 * 3;  // 6面 × 2三角形 × 3頂点
            int BlockIndexNum = blockSize * IndexNum;
            ushort[] BlockIndex = new ushort[BlockIndexNum];
            int ind = 0;
            ushort AddIndex = 0;
            for (int j = 0; j < blockSize; j++)
            {
                for (int i = 0; i < 12; i++)
                {
                    BlockIndex[ind] = (ushort)(ind + AddIndex);
                    ind++;
                    BlockIndex[ind] = (ushort)(ind + AddIndex);
                    ind++;
                    BlockIndex[ind] = (ushort)(ind + AddIndex);
                    ind++;
                }
                AddIndex += 24; // 1ブロック分の頂点数
            }

            indexBufferHandle = CreateIndexBuffer(BlockIndexNum, DX_INDEX_TYPE_16BIT);
            byte[] byteIndex = ConvertUShortArrayToByteArray(BlockIndex);
            SetIndexBufferData(0, byteIndex, BlockIndexNum, indexBufferHandle);
        }
    }
}
