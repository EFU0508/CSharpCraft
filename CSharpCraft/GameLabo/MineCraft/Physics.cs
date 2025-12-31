using System;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// ボクセルワールド用 物理・衝突判定クラス
    /// ・レイ vs ブロック(AABB)
    /// ・キャラクター用当たり判定
    /// ・地面検出
    /// </summary>
    public partial class Physics : IDisposable
    {
        // --- 衝突結果構造体 ---

        /// <summary>
        /// レイキャストの衝突結果
        /// </summary>
        public struct COLL_RESULT
        {
            public int HitFlag;                     // 衝突したか
            public VECTOR HitPosition;              // 衝突位置
            public VECTOR Normal;                   // 衝突面の法線
            public float Distance;                  // レイ開始点からの距離
            public (int x, int y, int z) BlockPos;  // ヒットしたブロック座標
            public ushort BlockId;                  // ブロックID
        }

        public Physics()
        {

        }

        public void Dispose()
        {

        }

        // --- DDA用ユーティリティ ---

        /// <summary>
        /// グリッド境界までの距離を計算（DDAアルゴリズム用）
        /// </summary>
        private float IntBound(float s, float ds)
        {
            if (ds > 0)
            {
                return (float)((Math.Floor(s + 1) - s) / ds);
            }
            else if (ds < 0)
            {
                return (float)((s - Math.Floor(s)) / -ds);
            }
            else
            {
                // その軸方向に進まない
                return float.MaxValue;
            }
        }

        // --- レイ vs AABB 判定 ---

        /// <summary>
        /// レイとAABB（1ブロック）との交差判定
        /// ・当たった場合はヒット位置・法線・距離を返す
        /// </summary>
        private bool LineAABB(
            VECTOR origin, VECTOR dir, float maxDist,
            VECTOR min, VECTOR max,
            out VECTOR hitPos, out VECTOR normal, out float hitDist)
        {
            hitPos = new VECTOR();
            normal = new VECTOR();
            hitDist = 0;

            float tmin = 0.0f, tmax = maxDist;

            // X/Y/Z 各軸についてスラブ判定
            for (int i = 0; i < 3; i++)
            {
                float o = (i == 0) ? origin.x : (i == 1) ? origin.y : origin.z;
                float d = (i == 0) ? dir.x : (i == 1) ? dir.y : dir.z;
                float minB = (i == 0) ? min.x : (i == 1) ? min.y : min.z;
                float maxB = (i == 0) ? max.x : (i == 1) ? max.y : max.z;

                // レイがその軸に平行な場合
                if (Math.Abs(d) < 1e-6f)
                {
                    if (o < minB || o > maxB) return false;
                }
                else
                {
                    float t1 = (minB - o) / d;
                    float t2 = (maxB - o) / d;
                    if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }

                    if (t1 > tmin) tmin = t1;
                    if (t2 < tmax) tmax = t2;

                    if (tmin > tmax) return false;
                }
            }

            // 範囲外
            if (tmin < 0 || tmin > maxDist) return false;

            // 衝突情報確定
            hitDist = tmin;
            hitPos = new VECTOR(origin.x + dir.x * tmin, origin.y + dir.y * tmin, origin.z + dir.z * tmin);

            // どの面に当たったかで法線を決定
            normal = new VECTOR(0, 0, 0);
            if (Math.Abs(hitPos.x - min.x) < 1e-3f) normal = new VECTOR(-1, 0, 0);
            else if (Math.Abs(hitPos.x - max.x) < 1e-3f) normal = new VECTOR(1, 0, 0);
            else if (Math.Abs(hitPos.y - min.y) < 1e-3f) normal = new VECTOR(0, -1, 0);
            else if (Math.Abs(hitPos.y - max.y) < 1e-3f) normal = new VECTOR(0, 1, 0);
            else if (Math.Abs(hitPos.z - min.z) < 1e-3f) normal = new VECTOR(0, 0, -1);
            else if (Math.Abs(hitPos.z - max.z) < 1e-3f) normal = new VECTOR(0, 0, 1);

            return true;
        }

        // --- レイキャスト（DDA） ---

        /// <summary>
        /// レイを飛ばしてワールド内ブロックとの衝突を調べる
        /// </summary>
        public COLL_RESULT LineVsAABB_WithNormal(
            VECTOR lineStart,
            VECTOR lineEnd)
        {
            COLL_RESULT result = new COLL_RESULT { HitFlag = FALSE };

            // レイ方向と長さ
            VECTOR dir = new VECTOR(lineEnd.x - lineStart.x, lineEnd.y - lineStart.y, lineEnd.z - lineStart.z);
            float length = (float)Math.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
            if (length <= 1e-6f) return result;

            dir.x /= length;
            dir.y /= length;
            dir.z /= length;

            // 現在のブロック座標
            int bx = (int)Math.Floor(lineStart.x);
            int by = (int)Math.Floor(lineStart.y);
            int bz = (int)Math.Floor(lineStart.z);

            // 各軸の進行方向
            int stepX = (dir.x > 0) ? 1 : -1;
            int stepY = (dir.y > 0) ? 1 : -1;
            int stepZ = (dir.z > 0) ? 1 : -1;

            // 次の境界までの距離
            float tMaxX = IntBound(lineStart.x, dir.x);
            float tMaxY = IntBound(lineStart.y, dir.y);
            float tMaxZ = IntBound(lineStart.z, dir.z);

            // 境界を1マス進むごとの距離
            float tDeltaX = (dir.x != 0) ? Math.Abs(1f / dir.x) : float.MaxValue;
            float tDeltaY = (dir.y != 0) ? Math.Abs(1f / dir.y) : float.MaxValue;
            float tDeltaZ = (dir.z != 0) ? Math.Abs(1f / dir.z) : float.MaxValue;

            float maxT = length;

            // DDAループ
            while (true)
            {
                // チャンク座標
                (int cx, int cy, int cz) = (bx >> 4, 0, bz >> 4);

                if (StClass.WRLD.chunks.dicChunks.TryGetValue((cx, cy, cz), out Chunks.Chunk chunk))
                {
                    int lx = bx & 0xF;
                    int ly = by;
                    int lz = bz & 0xF;

                    if (ly >= 0 && ly < 256)
                    {
                        ushort blockId = chunk.blockIds[lx, ly, lz];

                        // 空気・水・透明ブロックは無視
                        if ((blockId != Chunks.Block_Air) && (blockId != Chunks.Block_Water) && (blockId != Chunks.Block_Obs)) // 空気,水じゃなければ判定
                        {
                            VECTOR min = new VECTOR(bx, by, bz);
                            VECTOR max = new VECTOR(bx + 1, by + 1, bz + 1);

                            if (LineAABB(lineStart, dir, length, min, max, out VECTOR hitPos, out VECTOR normal, out float hitDist))
                            {
                                result.HitFlag = TRUE;
                                result.HitPosition = hitPos;
                                result.Normal = normal;
                                result.Distance = hitDist;
                                result.BlockPos = (bx, by, bz);
                                result.BlockId = blockId;
                                return result;
                            }
                        }
                    }
                }

                // 次に進む軸を決定
                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        if (tMaxX > maxT) break;
                        bx += stepX;
                        tMaxX += tDeltaX;
                    }
                    else
                    {
                        if (tMaxZ > maxT) break;
                        bz += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        if (tMaxY > maxT) break;
                        by += stepY;
                        tMaxY += tDeltaY;
                    }
                    else
                    {
                        if (tMaxZ > maxT) break;
                        bz += stepZ;
                        tMaxZ += tDeltaZ;
                    }
                }
            }

            return result;
        }

        // --- キャラクター衝突 ---

        /// <summary>
        /// 円柱（キャラ）とブロックの衝突判定
        /// </summary>
        public bool IsColliding(VECTOR pos, float radius, float height)
        {
            VECTOR min = new VECTOR(pos.x - radius, pos.y, pos.z - radius);
            VECTOR max = new VECTOR(pos.x + radius, pos.y + height, pos.z + radius);

            int bxMin = (int)Math.Floor(min.x);
            int byMin = (int)Math.Floor(min.y);
            int bzMin = (int)Math.Floor(min.z);
            int bxMax = (int)Math.Floor(max.x);
            int byMax = (int)Math.Floor(max.y);
            int bzMax = (int)Math.Floor(max.z);

            for (int bx = bxMin; bx <= bxMax; bx++)
            {
                for (int by = byMin; by <= byMax; by++)
                {
                    for (int bz = bzMin; bz <= bzMax; bz++)
                    {
                        if (IsBlockSolid(bx, by, bz))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 落下時の地面Y座標を取得
        /// </summary>
        public float? GetGroundY(VECTOR oldPos, VECTOR newPos, float radius)
        {
            if (newPos.y >= oldPos.y)
                return null;

            float startY = oldPos.y;
            float endY = newPos.y;

            int bxMin = (int)Math.Floor(newPos.x - radius);
            int bxMax = (int)Math.Floor(newPos.x + radius);
            int bzMin = (int)Math.Floor(newPos.z - radius);
            int bzMax = (int)Math.Floor(newPos.z + radius);

            for (int by = (int)Math.Floor(startY); by >= (int)Math.Floor(endY); by--)
            {
                for (int bx = bxMin; bx <= bxMax; bx++)
                {
                    for (int bz = bzMin; bz <= bzMax; bz++)
                    {
                        if (IsBlockSolid(bx, by, bz))
                        {
                            return by + 1.0f;
                        }
                    }
                }
            }

            return null;
        }

        // --- ブロック判定 ---

        /// <summary>
        /// 指定座標のブロックが「固体」かどうか
        /// </summary>
        private bool IsBlockSolid(int x, int y, int z)
        {
            int cx = (int)Math.Floor((float)x / 16f);
            int cy = (int)Math.Floor((float)y / 256f);
            int cz = (int)Math.Floor((float)z / 16f);

            if (!StClass.WRLD.chunks.dicChunks.TryGetValue((cx, cy, cz), out Chunks.Chunk chunk))
                return false;

            int lx = ((x % 16) + 16) % 16;
            int ly = ((y % 256) + 256) % 256;
            int lz = ((z % 16) + 16) % 16;

            return ((chunk.blockIds[lx, ly, lz] != Chunks.Block_Air) && (chunk.blockIds[lx, ly, lz] != Chunks.Block_Water) && (chunk.blockIds[lx, ly, lz] != Chunks.Block_Obs));
        }
    }
}
