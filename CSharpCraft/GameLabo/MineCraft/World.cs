using CmnDxlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CmnDxlib.Calc;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// ワールド全体（チャンク管理・ブロック操作・描画・エイム処理）を担当するクラス
    /// </summary>
    public class World : IDisposable
    {
#if DEBUG
        // デバッグ時の表示チャンク数（軽量）
        public static readonly int atlasViewNumMax = 5;
#else
        // リリース時の最大表示チャンク数
        public static readonly int atlasViewNumMax = 33;
#endif
        // 現在の表示チャンク幅（奇数推奨）
        public float atlasViewNum;

        // チャンク管理クラス
        public Chunks chunks;

        // チャンク保存用ファイルパス（フォーマット文字列）
        public string chunkFile;

        // =========================
        // ブロック配置キュー関連
        // =========================

        /// <summary>
        /// ブロック配置要求（遅延処理用）
        /// </summary>
        public struct BlockPlacement
        {
            public (int x, int y, int z) Position;
            public ushort BlockId;

            public BlockPlacement((int, int, int) pos, ushort id)
            {
                Position = pos;
                BlockId = id;
            }
        }

        // 1フレームでまとめて処理するためのキュー
        private Queue<BlockPlacement> blockPlacementQueue = new Queue<BlockPlacement>();

        /// <summary>
        /// ブロック配置をキューに積む（即時反映しない）
        /// </summary>
        public void EnqueueBlockPlacement((int, int, int) pos, ushort blockId)
        {
            blockPlacementQueue.Enqueue(new BlockPlacement(pos, blockId));
        }

        /// <summary>
        /// キューに溜まったブロック配置を少しずつ処理する
        /// </summary>
        public void ProcessBlockPlacements(int maxPerFrame = 10)
        {
            int count = 0;

            while (blockPlacementQueue.Count > 0 && count < maxPerFrame)
            {
                BlockPlacement bp = blockPlacementQueue.Dequeue();

                SetBlockInternal(bp.Position, bp.BlockId);

                count++;
            }
        }

        // =========================
        // チャンクロード管理
        // =========================

        // 現在必要なチャンク座標
        public HashSet<(int, int, int)> neededChunks = new HashSet<(int, int, int)>();

        // 生成待ちチャンク
        public Queue<(int, int, int)> pendingChunks = new Queue<(int, int, int)>();

        public World()
        {
            chunks = new Chunks();

            // AppData以下にチャンク保存フォルダを作成
            chunkFile = FileFunc.GetAppDataLocalFolder();
            chunkFile = System.IO.Path.Combine(chunkFile, $"chunks_dat_{StClass.StageID}");
            if (!Directory.Exists(chunkFile))
            {
                Directory.CreateDirectory(chunkFile);
            }

            // 実際のチャンクファイル名フォーマット
            chunkFile = System.IO.Path.Combine(chunkFile, "r_{0}_{1}\\chunk_{2}_{3}.dat");
        }

        public void Dispose()
        {
            pendingChunks.Clear();
            neededChunks.Clear();

            chunks?.Dispose();
            chunks = null;
        }

        // =========================
        // チャンクデータ生成・読込
        // =========================

        /// <summary>
        /// チャンクデータをファイルから取得、なければ新規生成
        /// </summary>
        public ushort[,,] GetAtlas(int stageid, (int, int, int) pos)
        {
            int offsetX = pos.Item1;
            int offsetY = pos.Item2;
            int offsetZ = pos.Item3;
            string fileName = string.Format(chunkFile, FloorDiv(offsetX, 32), FloorDiv(offsetZ, 32), offsetX, offsetZ);

            ushort[,,] chunkData;

            // 既存チャンクがあればロード
            if (File.Exists(fileName))
            {
                chunkData = FileFunc.LoadFastUshort(fileName);
            }
            else
            {
                // 新規チャンク生成
                string dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // ステージによる地面ブロック切り替え
                ushort defaultBlockId = Chunks.Block_GoodSoil;
                if (stageid == 1)
                {
                    defaultBlockId = Chunks.Block_BadSoil;
                }

                chunkData = new ushort[Chunks.blockX, Chunks.blockY, Chunks.blockZ];
                // 下は地面、上は空気
                for (int z = 0; z < Chunks.blockZ; z++)
                {
                    for (int y = 0; y < Chunks.blockY; y++)
                    {
                        for (int x = 0; x < Chunks.blockX; x++)
                        {
                            ushort u;
                            if (y < 128)
                            {
                                u = defaultBlockId;
                            }
                            else
                            {
                                u = Chunks.Block_Air;
                            }
                            chunkData[x, y, z] = u;
                        }
                    }
                }
            }

            return chunkData;
        }

        /// <summary>
        /// 指定チャンク座標のデータを生成・登録
        /// </summary>
        public void MakeAtlas((int, int, int) pos)
        {
            Chunks.Chunk chunk;

            if (StClass.WRLD.chunks.dicChunks.ContainsKey(pos))
            {
                chunk = StClass.WRLD.chunks.dicChunks[pos];
            }
            else
            {
                chunk = new Chunks.Chunk();
            }

            chunk.blockIds = GetAtlas(StClass.StageID, pos);
            StClass.WRLD.chunks.dicChunks[pos] = chunk;
        }

        // =========================
        // ブロック取得
        // =========================
        public ushort GetWorldBlockId(VECTOR pos)
        {
            return GetWorldBlockId(((int)Math.Floor(pos.x), (int)Math.Floor(pos.y), (int)Math.Floor(pos.z)));
        }

        public ushort GetWorldBlockId((int, int, int) pos)
        {
            int chunkX = FloorDiv(pos.Item1, Chunks.blockX);
            int chunkY = FloorDiv(pos.Item2, Chunks.blockY);
            int chunkZ = FloorDiv(pos.Item3, Chunks.blockZ);
            (int, int, int) chunkPos = (chunkX, chunkY, chunkZ);

            if (!StClass.WRLD.chunks.dicChunks.ContainsKey(chunkPos)) return ushort.MaxValue;

            Chunks.Chunk chunk = StClass.WRLD.chunks.dicChunks[chunkPos];
            int localX = Mod(pos.Item1, Chunks.blockX);
            int localY = Mod(pos.Item2, Chunks.blockY);
            int localZ = Mod(pos.Item3, Chunks.blockZ);
            return chunk.blockIds[localX, localY, localZ];
        }

        // =========================
        // チャンク更新・描画
        // =========================

        /// <summary>
        /// プレイヤー位置を中心に必要なチャンクを管理
        /// </summary>
        public void UpdateChunks(VECTOR playerPos)
        {
            int chunkX = FloorDiv((int)playerPos.x, Chunks.blockX);
            int chunkY = FloorDiv((int)playerPos.y, Chunks.blockY);
            int chunkZ = FloorDiv((int)playerPos.z, Chunks.blockZ);
            (int, int, int) center = (chunkX, chunkY, chunkZ);

            int half = (int)StClass.WRLD.atlasViewNum / 2;
            HashSet<(int, int, int)> newNeeded = new HashSet<(int, int, int)>();

            // 必要チャンク範囲を算出
            for (int dx = -half; dx <= half; dx++)
                for (int dz = -half; dz <= half; dz++)
                {
                    newNeeded.Add((center.Item1 + dx, center.Item2, center.Item3 + dz));
                }

            // 新規に必要なチャンクをキューへ
            foreach (var pos in newNeeded)
            {
                if (!StClass.WRLD.chunks.dicChunks.ContainsKey(pos) && !pendingChunks.Contains(pos))
                {
                    pendingChunks.Enqueue(pos);
                }
            }

            // 不要になったチャンクを破棄
            var toRemove = StClass.WRLD.chunks.dicChunks.Keys
                             .Where(p => !newNeeded.Contains(p))
                             .ToList();
            foreach (var pos in toRemove)
            {
                var chunk = StClass.WRLD.chunks.dicChunks[pos];
                if (chunk.vertexBufferHandle != 0)
                    DeleteVertexBuffer(chunk.vertexBufferHandle);
                StClass.WRLD.chunks.dicChunks.Remove(pos);
            }

            neededChunks = newNeeded;

            // 1フレームに生成するチャンク数制限
            int maxPerFrame = 1;
            for (int i = 0; i < maxPerFrame && pendingChunks.Count > 0; i++)
            {
                var pos = pendingChunks.Dequeue();

                if (!neededChunks.Contains(pos)) continue;

                MakeAtlas(pos);
                StClass.WRLD.chunks.CreateVertex(pos);
            }
        }

        /// <summary>
        /// プレイヤー周囲のチャンクを描画
        /// </summary>
        public void DrawAroundPlayer(VECTOR playerPos)
        {
            int chunkX = FloorDiv((int)playerPos.x, Chunks.blockX);
            int chunkY = FloorDiv((int)playerPos.y, Chunks.blockY);
            int chunkZ = FloorDiv((int)playerPos.z, Chunks.blockZ);

            int half = (int)StClass.WRLD.atlasViewNum / 2;

            for (int dx = -half; dx <= half; dx++)
            {
                for (int dz = -half; dz <= half; dz++)
                {
                    (int, int, int) pos = (chunkX + dx, chunkY, chunkZ + dz);
                    if (StClass.WRLD.chunks.dicChunks.ContainsKey(pos))
                    {
                        DrawPolygon3D_UseVertexBuffer(StClass.WRLD.chunks.dicChunks[pos].vertexBufferHandle,
                                                      StClass.DAT.blockTextures, TRUE);
                    }
                }
            }
        }

        // =========================
        // エイム・ブロック選択表示
        // =========================

        /// <summary>
        /// 選択中ブロックのワイヤーフレーム描画
        /// </summary>
        private void DrawSelectedBlock()
        {
            (int, int, int) blockPos;
            if (StClass.DAT.selectBlockID == 0)
            {
                blockPos = aimTarget.Value;
            }
            else
            {
                blockPos = aimPrev.Value;
            }

            int bx = blockPos.Item1;
            int by = blockPos.Item2;
            int bz = blockPos.Item3;

            VECTOR[] v = new VECTOR[8];
            v[0] = VGet(bx, by, bz);
            v[1] = VGet(bx + 1, by, bz);
            v[2] = VGet(bx, by + 1, bz);
            v[3] = VGet(bx + 1, by + 1, bz);
            v[4] = VGet(bx, by, bz + 1);
            v[5] = VGet(bx + 1, by, bz + 1);
            v[6] = VGet(bx, by + 1, bz + 1);
            v[7] = VGet(bx + 1, by + 1, bz + 1);

            uint col = GetColor(255, 255, 255);

            // 各辺を線で描画
            DrawLine3D(v[0], v[1], col);
            DrawLine3D(v[1], v[5], col);
            DrawLine3D(v[5], v[4], col);
            DrawLine3D(v[4], v[0], col);

            DrawLine3D(v[2], v[3], col);
            DrawLine3D(v[3], v[7], col);
            DrawLine3D(v[7], v[6], col);
            DrawLine3D(v[6], v[2], col);

            DrawLine3D(v[0], v[2], col);
            DrawLine3D(v[1], v[3], col);
            DrawLine3D(v[5], v[7], col);
            DrawLine3D(v[4], v[6], col);
        }

        // =========================
        // エイム処理
        // =========================

        private (int, int, int)? aimTarget; // 狙っているブロック
        private (int, int, int)? aimPrev;   // その一つ手前
        private VECTOR screenPos;           // クロスヘア描画位置

        /// <summary>
        /// レイを飛ばしてブロックを取得
        /// </summary>
        private void GetTargetBlock(VECTOR playerPos, VECTOR dir, float maxDistance = 5.0f)
        {
            VECTOR pos = playerPos;
            int prevBx = (int)Math.Floor(pos.x);
            int prevBy = (int)Math.Floor(pos.y);
            int prevBz = (int)Math.Floor(pos.z);

            for (float t = 0; t < maxDistance; t += 0.1f)
            {
                pos.x += dir.x * 0.1f;
                pos.y += dir.y * 0.1f;
                pos.z += dir.z * 0.1f;

                int bx = (int)Math.Floor(pos.x);
                int by = (int)Math.Floor(pos.y);
                int bz = (int)Math.Floor(pos.z);

                ushort blockId = GetWorldBlockId((bx, by, bz));
                if (blockId != ushort.MaxValue)
                {
                    int dx = bx - prevBx;
                    int dy = by - prevBy;
                    int dz = bz - prevBz;

                    aimTarget = (bx, by, bz);
                    aimPrev = (prevBx, prevBy, prevBz);
                    return;
                }

                prevBx = bx;
                prevBy = by;
                prevBz = bz;
            }

            screenPos = ConvWorldPosToScreenPos(pos);

            aimTarget = null;
            aimPrev = null;
            return;
        }

        /// <summary>
        /// ブロックが無い場合の十字カーソル表示
        /// </summary>
        private void CrosshairView()
        {
            int centerX = (int)screenPos.x;
            int centerY = (int)screenPos.y;

            int size = 10;
            uint col = GetColor(255, 255, 255);
            DrawLine(centerX - size, centerY, centerX + size, centerY, col);
            DrawLine(centerX, centerY - size, centerX, centerY + size, col);
        }

        public void UpdateAim(VECTOR position, float height, float distance)
        {
            VECTOR rayStart = StClass.DAT.modelInfo[StClass.UserID].Position;
            rayStart.y += StClass.DAT.modelInfo[StClass.UserID].Height;
            VECTOR dir = GetCameraFrontVector();
            GetTargetBlock(rayStart, dir, distance);
        }

        public void DrawAim()
        {
            if (aimTarget.HasValue)
            {
                StClass.WRLD.DrawSelectedBlock();
            }
            else
            {
                CrosshairView();
            }
        }

        // =========================
        // ブロック設置・削除
        // =========================

        /// <summary>
        /// 内部的にブロックを書き換える（即反映＋保存）
        /// </summary>
        public void SetBlockInternal((int, int, int) pos, ushort blockId)
        {
            int chunkX = FloorDiv(pos.Item1, Chunks.blockX);
            int chunkY = FloorDiv(pos.Item2, Chunks.blockY);
            int chunkZ = FloorDiv(pos.Item3, Chunks.blockZ);
            (int, int, int) chunkPos = (chunkX, chunkY, chunkZ);

            if (!StClass.WRLD.chunks.dicChunks.ContainsKey(chunkPos))
                return;

            Chunks.Chunk chunk = StClass.WRLD.chunks.dicChunks[chunkPos];

            int localX = Mod(pos.Item1, Chunks.blockX);
            int localY = Mod(pos.Item2, Chunks.blockY);
            int localZ = Mod(pos.Item3, Chunks.blockZ);

            chunk.blockIds[localX, localY, localZ] = blockId;
            StClass.WRLD.chunks.dicChunks[chunkPos] = chunk;

            // 境界ブロックなら隣接チャンクも更新
            List<(int, int, int)> needUpdate = new List<(int, int, int)>();
            needUpdate.Add(chunkPos);

            if (localX == 0) needUpdate.Add((chunkX - 1, chunkY, chunkZ));
            if (localX == Chunks.blockX - 1) needUpdate.Add((chunkX + 1, chunkY, chunkZ));
            if (localY == 0) needUpdate.Add((chunkX, chunkY - 1, chunkZ));
            if (localY == Chunks.blockY - 1) needUpdate.Add((chunkX, chunkY + 1, chunkZ));
            if (localZ == 0) needUpdate.Add((chunkX, chunkY, chunkZ - 1));
            if (localZ == Chunks.blockZ - 1) needUpdate.Add((chunkX, chunkY, chunkZ + 1));

            foreach (var cpos in needUpdate)
            {
                if (StClass.WRLD.chunks.dicChunks.ContainsKey(cpos))
                {
                    StClass.WRLD.chunks.CreateVertex(cpos);
                }
            }

            {
                // チャンク保存
                string fileName = string.Format(chunkFile, FloorDiv(chunkX, 32), FloorDiv(chunkZ, 32), chunkX, chunkZ);
                string dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                FileFunc.SaveFastUshort(fileName, chunk.blockIds);
            }
        }

        /// <summary>
        /// ブロック削除
        /// </summary>
        public (int, int, int)? RemoveBlock()
        {
            if (!aimTarget.HasValue)
            {
                return null;
            }

            {
                StClass.DAT.physics.CheckPortalBlockOff(aimTarget.Value);
                EnqueueBlockPlacement(aimTarget.Value, Chunks.Block_Air);
            }

            return aimTarget.Value;
        }

        /// <summary>
        /// ブロック設置
        /// </summary>
        public (int, int, int)? PlaceBlock(ushort blockId, VECTOR pos)
        {
            if (!aimPrev.HasValue)
            {
                return null;
            }

            int bx = (int)Math.Floor(pos.x);
            int by = (int)Math.Floor(pos.y);
            int bz = (int)Math.Floor(pos.z);

            // 自分の位置に置けないように制限
            if (aimPrev.Value == (bx, by, bz))
            {
                return null;
            }
            if (aimPrev.Value == (bx, by + 1, bz))
            {
                return null;
            }

            if (GetWorldBlockId(aimPrev.Value) == Chunks.Block_Air)
            {
                SetBlockInternal(aimPrev.Value, blockId);
                StClass.DAT.physics.CheckPortalBlockOn(aimPrev.Value);
            }

            return aimPrev.Value;
        }

        // =========================
        // 初期ワールド生成
        // =========================
        public void InitWorld()
        {
            StClass.WRLD.chunks.CreateIndex();

            (int, int, int) pos = (
                (int)(StClass.DAT.modelInfo[StClass.UserID].Position.x / Chunks.blockX),
                0,
                (int)(StClass.DAT.modelInfo[StClass.UserID].Position.z / Chunks.blockZ)
                );

            StClass.WRLD.atlasViewNum = 3;
            int atlasViewNum = (int)StClass.WRLD.atlasViewNum;

            // チャンク生成
            for (int z = 0; z < atlasViewNum; z++)
            {
                int zz = z + pos.Item3;
                for (int x = 0; x < StClass.WRLD.atlasViewNum; x++)
                {
                    int xx = x + pos.Item1;
                    StClass.WRLD.MakeAtlas((xx - (atlasViewNum / 2), 0, zz - (atlasViewNum / 2)));
                }
            }

            // 頂点生成
            for (int z = 0; z < StClass.WRLD.atlasViewNum; z++)
            {
                int zz = z + pos.Item3;
                for (int x = 0; x < StClass.WRLD.atlasViewNum; x++)
                {
                    int xx = x + pos.Item1;
                    StClass.WRLD.chunks.CreateVertex((xx - (atlasViewNum / 2), 0, zz - (atlasViewNum / 2)));
                }
            }
        }
    }
}
