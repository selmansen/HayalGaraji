using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Chibi (şişirilmiş oyuncak) şekiller için mesh üreticileri:
    /// süper-elips kesitli gövde (loft), dönel yüzeyler (lastik, jant, platform), elipsoid, birleştirme.
    /// Tüm yüzeyler yumuşak normallerle üretilir; dış çizgi kırılmaz.
    /// </summary>
    public static class ChibiMesh
    {
        public struct Section { public float z, halfW, center, halfH; }

        public class LoftData
        {
            public Vector3[] v, n; public Vector2[] uv; public List<int> tris; public int rings, cols;
        }

        public static Vector2 SuperEllipse(float th, float a, float b, float n)
        {
            float c = Mathf.Cos(th), s = Mathf.Sin(th), e = 2f / n;
            return new Vector2(a * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e), b * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e));
        }

        /// <summary>Uçlarda sık, ortada seyrek örnekleme (yuvarlak uçlar pürüzsüz olsun).</summary>
        public static float CosSpace(int i, int count) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * i / (count - 1));

        public static float Band(float f, float s, float e, float w) => Mathf.SmoothStep(0, 1, (f - s + w) / (2 * w)) - Mathf.SmoothStep(0, 1, (f - e + w) / (2 * w));

        // ---------------- loft ----------------

        /// <summary>Kesitleri arkadan öne doğru birleştirir. Dikiş yeri altta (görünmez), UV: u=çevre, v=boy.</summary>
        public static LoftData Loft(IList<Section> secs, int around, float exp)
        {
            int N = secs.Count, C = around + 1;
            var v = new Vector3[N * C]; var uv = new Vector2[N * C];
            for (int i = 0; i < N; i++)
            {
                var s = secs[i];
                for (int j = 0; j <= around; j++)
                {
                    float th = -Mathf.PI / 2f + 2f * Mathf.PI * j / around;
                    var p = SuperEllipse(th, s.halfW, s.halfH, exp);
                    v[i * C + j] = new Vector3(p.x, s.center + p.y, s.z);
                    uv[i * C + j] = new Vector2((float)j / around, (float)i / (N - 1));
                }
            }
            var tris = new List<int>();
            for (int i = 0; i < N - 1; i++)
                for (int j = 0; j < around; j++)
                {
                    int a = i * C + j, b = a + 1, c = a + C, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            int mi = N / 2, ta = mi * C + around / 4;
            var nrm = Vector3.Cross(v[ta + C] - v[ta], v[ta + 1] - v[ta]);
            if (Vector3.Dot(nrm, v[ta] - new Vector3(0, secs[mi].center, secs[mi].z)) < 0) Flip(tris);

            var n = ComputeNormals(v, tris);
            for (int i = 0; i < N; i++)
            {
                int f = i * C, l = f + around;
                var m = (n[f] + n[l]).normalized; n[f] = m; n[l] = m;
                if (secs[i].halfW < 1e-4f || secs[i].halfH < 1e-4f)
                {
                    var sum = Vector3.zero;
                    for (int j = 0; j <= around; j++) sum += n[f + j];
                    sum.Normalize();
                    for (int j = 0; j <= around; j++) n[f + j] = sum;
                }
            }
            return new LoftData { v = v, n = n, uv = uv, tris = tris, rings = N, cols = C };
        }

        /// <summary>Loft üçgenlerini gruplara ayırır (ör. kabin: kemer / cam / tavan).</summary>
        public static List<int>[] Split(LoftData d, int groups, Func<Vector3, float, int> classify)
        {
            var res = new List<int>[groups];
            for (int g = 0; g < groups; g++) res[g] = new List<int>();
            for (int k = 0; k < d.tris.Count; k += 3)
            {
                int a = d.tris[k], b = d.tris[k + 1], c = d.tris[k + 2];
                var centroid = (d.v[a] + d.v[b] + d.v[c]) / 3f;
                float t = (float)(a / d.cols) / (d.rings - 1);
                int g = Mathf.Clamp(classify(centroid, t), 0, groups - 1);
                res[g].Add(a); res[g].Add(b); res[g].Add(c);
            }
            return res;
        }

        /// <summary>
        /// Loft'u kesit açısına göre böler (kesit içindeki konum: alt / yan / üst).
        /// Sınırlar mesh kenarlarından geçtiği için basamaklı (tırtıklı) görünmez.
        /// classify(sinTheta) → grup; sinTheta -1 = tam alt, +1 = tam üst.
        /// </summary>
        public static List<int>[] SplitByAngle(LoftData d, int groups, Func<float, int> classify)
        {
            int around = d.cols - 1;
            var res = new List<int>[groups];
            for (int g = 0; g < groups; g++) res[g] = new List<int>();
            for (int k = 0; k < d.tris.Count; k += 6)
            {
                int a = d.tris[k], b = d.tris[k + 1], c = d.tris[k + 2];
                int col = Mathf.Min(a % d.cols, Mathf.Min(b % d.cols, c % d.cols));
                float th = -Mathf.PI / 2f + 2f * Mathf.PI * (col + 0.5f) / around;
                int grp = Mathf.Clamp(classify(Mathf.Sin(th)), 0, groups - 1);
                for (int q = 0; q < 6 && k + q < d.tris.Count; q++) res[grp].Add(d.tris[k + q]);
            }
            return res;
        }

        // ---------------- dönel yüzey ----------------

        /// <summary>
        /// Profili eksen etrafında döndürür. Profil: x = eksen boyunca, y = yarıçap.
        /// axisY=false → eksen X (tekerlek), true → eksen Y (platform).
        /// radialOffset(phi, profilIndex) yarıçapa ekleme yapar (diş deseni, dişli).
        /// </summary>
        public static Mesh Revolve(string name, IList<Vector2> profile, bool closed, int segs, bool axisY = false, Func<float, int, float> radialOffset = null)
        {
            int P = profile.Count;
            var v = new Vector3[P * segs];
            for (int k = 0; k < P; k++)
                for (int j = 0; j < segs; j++)
                {
                    float phi = 2f * Mathf.PI * j / segs;
                    float r = Mathf.Max(0f, profile[k].y + (radialOffset != null ? radialOffset(phi, k) : 0f));
                    float ax = profile[k].x, c = Mathf.Cos(phi), s = Mathf.Sin(phi);
                    v[k * segs + j] = axisY ? new Vector3(r * c, ax, r * s) : new Vector3(ax, r * c, r * s);
                }
            var tris = new List<int>();
            int K = closed ? P : P - 1;
            for (int k = 0; k < K; k++)
            {
                int k2 = (k + 1) % P;
                for (int j = 0; j < segs; j++)
                {
                    int j2 = (j + 1) % segs;
                    int a = k * segs + j, b = k * segs + j2, c = k2 * segs + j, d = k2 * segs + j2;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            // En geniş noktada dışa bakıyor mu?
            int km = 0;
            for (int k = 1; k < P; k++) if (profile[k].y > profile[km].y) km = k;
            if (!closed && km == P - 1) km--;
            int ia = km * segs, ic = ((km + 1) % P) * segs, ib = km * segs + 1;
            var nrm = Vector3.Cross(v[ic] - v[ia], v[ib] - v[ia]);
            var A = v[ia];
            var outward = axisY ? new Vector3(A.x, 0, A.z) : new Vector3(0, A.y, A.z);
            if (Vector3.Dot(nrm, outward) < 0) Flip(tris);
            return ToMesh(name, v, ComputeNormals(v, tris), null, tris);
        }

        public static List<Vector2> Circle(Vector2 center, float radius, int pts, float from = 0f, float to = Mathf.PI * 2f, bool closedLoop = true)
        {
            var l = new List<Vector2>();
            int count = closedLoop ? pts : pts + 1;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(from, to, closedLoop ? (float)i / pts : (float)i / pts);
                l.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
            return l;
        }

        // ---------------- elipsoid ----------------

        public static Mesh Ellipsoid(Vector3 radii, int lat = 14, int lon = 24)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>();
            void Add(Vector3 p)
            {
                v.Add(p);
                n.Add(new Vector3(p.x / (radii.x * radii.x), p.y / (radii.y * radii.y), p.z / (radii.z * radii.z)).normalized);
            }
            Add(new Vector3(0, radii.y, 0));
            for (int i = 1; i < lat; i++)
            {
                float ph = Mathf.PI * i / lat, y = Mathf.Cos(ph), rr = Mathf.Sin(ph);
                for (int j = 0; j < lon; j++)
                {
                    float th = 2f * Mathf.PI * j / lon;
                    Add(new Vector3(rr * Mathf.Cos(th) * radii.x, y * radii.y, rr * Mathf.Sin(th) * radii.z));
                }
            }
            Add(new Vector3(0, -radii.y, 0));
            int bottom = v.Count - 1;
            var tris = new List<int>();
            for (int j = 0; j < lon; j++) { int j2 = (j + 1) % lon; tris.Add(0); tris.Add(1 + j2); tris.Add(1 + j); }
            for (int i = 0; i < lat - 2; i++)
                for (int j = 0; j < lon; j++)
                {
                    int j2 = (j + 1) % lon, a = 1 + i * lon + j, b = 1 + i * lon + j2, c = a + lon, d = b + lon;
                    tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c);
                }
            int last = 1 + (lat - 2) * lon;
            for (int j = 0; j < lon; j++) { int j2 = (j + 1) % lon; tris.Add(bottom); tris.Add(last + j); tris.Add(last + j2); }
            var va = v.ToArray();
            FixWindingByCenter(va, tris, Vector3.zero);
            return ToMesh("Ellipsoid", va, n.ToArray(), null, tris);
        }

        // ---------------- disk (şeker spirali gibi iki renkli yüzeyler) ----------------

        /// <summary>Hafif kubbeli, X eksenine bakan disk. classify(açı, 0..1 yarıçap) → grup.</summary>
        public static Mesh[] DomedDisc(float x, float sign, float radius, float dome, int rings, int segs, Func<float, float, int> classify, int groups)
        {
            var v = new List<Vector3>();
            v.Add(new Vector3(sign * (x + dome), 0, 0));
            for (int i = 1; i <= rings; i++)
            {
                float rn = (float)i / rings, r = rn * radius, px = sign * (x + dome * (1 - rn * rn));
                for (int j = 0; j < segs; j++)
                {
                    float a = 2f * Mathf.PI * j / segs;
                    v.Add(new Vector3(px, Mathf.Cos(a) * r, Mathf.Sin(a) * r));
                }
            }
            var all = new List<int>(); var owner = new List<int>();
            for (int j = 0; j < segs; j++)
            {
                int j2 = (j + 1) % segs;
                all.Add(0); all.Add(1 + j); all.Add(1 + j2);
                owner.Add(classify((j + 0.5f) / segs * Mathf.PI * 2f, 0.5f / rings));
            }
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < segs; j++)
                {
                    int j2 = (j + 1) % segs, a = 1 + i * segs + j, b = 1 + i * segs + j2, c = a + segs, d = b + segs;
                    int g = classify((j + 0.5f) / segs * Mathf.PI * 2f, (i + 1f) / rings);
                    all.Add(a); all.Add(c); all.Add(b); owner.Add(g);
                    all.Add(b); all.Add(c); all.Add(d); owner.Add(g);
                }
            var va = v.ToArray();
            // Yön: +X (sign) tarafına baksın
            var tA = va[all[0]]; var nrm = Vector3.Cross(va[all[1]] - tA, va[all[2]] - tA);
            bool flip = Vector3.Dot(nrm, new Vector3(sign, 0, 0)) < 0;
            if (flip) for (int k = 0; k < all.Count; k += 3) { int t = all[k + 1]; all[k + 1] = all[k + 2]; all[k + 2] = t; }
            var normals = ComputeNormals(va, all);
            var res = new Mesh[groups];
            for (int g = 0; g < groups; g++)
            {
                var sub = new List<int>();
                for (int k = 0; k < owner.Count; k++) if (owner[k] == g) { sub.Add(all[k * 3]); sub.Add(all[k * 3 + 1]); sub.Add(all[k * 3 + 2]); }
                res[g] = ToMesh("Disc" + g, va, normals, null, sub);
            }
            return res;
        }

        // ---------------- düzlemsel boru (şekil çerçeveleri) ----------------

        /// <summary>XY düzleminde kapalı bir şeklin çevresinden geçen yuvarlak boru.</summary>
        public static Mesh TubeLoop(string name, IList<Vector2> pts, float radius, int ring = 12)
        {
            int N = pts.Count;
            var v = new Vector3[N * ring];
            for (int i = 0; i < N; i++)
            {
                var p = pts[i];
                var t = (pts[(i + 1) % N] - pts[(i - 1 + N) % N]).normalized;
                var n = new Vector3(-t.y, t.x, 0f);
                for (int j = 0; j < ring; j++)
                {
                    float a = 2f * Mathf.PI * j / ring;
                    v[i * ring + j] = new Vector3(p.x, p.y, 0f) + (n * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a)) * radius;
                }
            }
            var tris = new List<int>();
            for (int i = 0; i < N; i++)
            {
                int i2 = (i + 1) % N;
                for (int j = 0; j < ring; j++)
                {
                    int j2 = (j + 1) % ring, a = i * ring + j, b = i * ring + j2, c = i2 * ring + j, d = i2 * ring + j2;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            // Dışa baksın: ilk halkanın ilk noktası borunun merkezinden dışarı
            var center0 = new Vector3(pts[0].x, pts[0].y, 0f);
            var nrm = Vector3.Cross(v[ring] - v[0], v[1] - v[0]);
            if (Vector3.Dot(nrm, v[0] - center0) < 0) Flip(tris);
            return ToMesh(name, v, ComputeNormals(v, tris), null, tris);
        }

        /// <summary>Köşeleri yuvarlatılmış çokgenin çevre noktaları.</summary>
        public static List<Vector2> RoundedOutline(IList<Vector2> poly, float r, int perCorner = 8, int perEdge = 6)
        {
            var res = new List<Vector2>();
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = poly[(i - 1 + n) % n], p1 = poly[i], p2 = poly[(i + 1) % n];
                Vector2 d1 = (p0 - p1).normalized, d2 = (p2 - p1).normalized;
                float rr = Mathf.Min(r, (p0 - p1).magnitude * 0.45f, (p2 - p1).magnitude * 0.45f);
                Vector2 a = p1 + d1 * rr, b = p1 + d2 * rr;
                for (int k = 0; k <= perCorner; k++)
                {
                    float t = (float)k / perCorner;
                    res.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * p1 + t * t * b);
                }
                Vector2 next = poly[(i + 1) % n], after = poly[(i + 2) % n];
                float rn = Mathf.Min(r, (p1 - next).magnitude * 0.45f, (after - next).magnitude * 0.45f);
                Vector2 edgeEnd = next + (p1 - next).normalized * rn;
                for (int k = 1; k < perEdge; k++) res.Add(Vector2.Lerp(b, edgeEnd, (float)k / perEdge));
            }
            return res;
        }

        // ---------------- yardımcılar ----------------

        public static Mesh Combine(string name, List<(Mesh mesh, Matrix4x4 xf)> parts, bool destroySources = true)
        {
            var ci = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++) ci[i] = new CombineInstance { mesh = parts[i].mesh, transform = parts[i].xf };
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.CombineMeshes(ci, true, true);
            m.RecalculateBounds();
            if (destroySources) foreach (var p in parts) UnityEngine.Object.DestroyImmediate(p.mesh);
            return m;
        }

        public static Matrix4x4 TRS(Vector3 p, Quaternion r) => Matrix4x4.TRS(p, r, Vector3.one);
        public static Matrix4x4 At(Vector3 p) => Matrix4x4.Translate(p);

        public static Mesh ToMesh(string name, Vector3[] v, Vector3[] n, Vector2[] uv, List<int> tris)
        {
            var m = new Mesh { name = name };
            if (v.Length > 65000) m.indexFormat = IndexFormat.UInt32;
            m.vertices = v; m.normals = n;
            if (uv != null) m.uv = uv;
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            if (uv != null) m.RecalculateTangents();
            return m;
        }

        public static Vector3[] ComputeNormals(Vector3[] v, List<int> tris)
        {
            var n = new Vector3[v.Length];
            for (int k = 0; k < tris.Count; k += 3)
            {
                int a = tris[k], b = tris[k + 1], c = tris[k + 2];
                var f = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                n[a] += f; n[b] += f; n[c] += f;
            }
            for (int i = 0; i < n.Length; i++) n[i] = n[i].sqrMagnitude > 1e-12f ? n[i].normalized : Vector3.up;
            return n;
        }

        static void Flip(List<int> tris)
        {
            for (int k = 0; k < tris.Count; k += 3) { int t = tris[k + 1]; tris[k + 1] = tris[k + 2]; tris[k + 2] = t; }
        }

        static void FixWindingByCenter(Vector3[] v, List<int> tris, Vector3 center)
        {
            float best = 0; int bk = 0;
            for (int k = 0; k < Mathf.Min(tris.Count, 600); k += 3)
            {
                float area = Vector3.Cross(v[tris[k + 1]] - v[tris[k]], v[tris[k + 2]] - v[tris[k]]).sqrMagnitude;
                if (area > best) { best = area; bk = k; }
            }
            var a = v[tris[bk]];
            var nrm = Vector3.Cross(v[tris[bk + 1]] - a, v[tris[bk + 2]] - a);
            var cen = (a + v[tris[bk + 1]] + v[tris[bk + 2]]) / 3f;
            if (Vector3.Dot(nrm, cen - center) < 0) Flip(tris);
        }
    }
}
