using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LoogaSoft.UIFX
{
    static class LoogaUIShapeMeshUtility
    {
        const float MinimumSqrDistance = 0.0001f;

        static readonly List<int> Indices = new();

        public static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 colorA, Color32 colorB, Color32 colorC)
        {
            int index = vh.currentVertCount;
            vh.AddVert(a, colorA, Vector2.zero);
            vh.AddVert(b, colorB, Vector2.zero);
            vh.AddVert(c, colorC, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
        }

        public static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            int index = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero);
            vh.AddVert(b, color, Vector2.zero);
            vh.AddVert(c, color, Vector2.zero);
            vh.AddVert(d, color, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }

        public static void AddDisc(VertexHelper vh, Rect rect, Vector2 center, float outerRadius, float innerRadius, float startAngle, float arcDegrees, int segments, ShapeColorEvaluator colorEvaluator)
        {
            segments = Mathf.Max(3, segments);
            arcDegrees = Mathf.Clamp(arcDegrees, 0f, 360f);
            if (outerRadius <= 0f || arcDegrees <= 0f)
            {
                return;
            }

            innerRadius = Mathf.Clamp(innerRadius, 0f, outerRadius);
            int steps = Mathf.Max(1, Mathf.CeilToInt(segments * (arcDegrees / 360f)));
            float step = arcDegrees / steps;

            for (int i = 0; i < steps; i++)
            {
                float angleA = (startAngle + step * i) * Mathf.Deg2Rad;
                float angleB = (startAngle + step * (i + 1)) * Mathf.Deg2Rad;
                Vector2 outerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * outerRadius;
                Vector2 outerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * outerRadius;

                if (innerRadius <= 0.001f)
                {
                    AddTriangle(vh, center, outerA, outerB, colorEvaluator(center, rect), colorEvaluator(outerA, rect), colorEvaluator(outerB, rect));
                    continue;
                }

                Vector2 innerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * innerRadius;
                Vector2 innerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * innerRadius;
                int index = vh.currentVertCount;
                vh.AddVert(innerA, colorEvaluator(innerA, rect), Vector2.zero);
                vh.AddVert(outerA, colorEvaluator(outerA, rect), Vector2.zero);
                vh.AddVert(outerB, colorEvaluator(outerB, rect), Vector2.zero);
                vh.AddVert(innerB, colorEvaluator(innerB, rect), Vector2.zero);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index + 2, index + 3, index);
            }
        }

        public static void AddPolygonFill(VertexHelper vh, Rect rect, IReadOnlyList<Vector2> points, ShapeColorEvaluator colorEvaluator)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            Indices.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0 || (points[i] - points[i - 1]).sqrMagnitude > MinimumSqrDistance)
                {
                    Indices.Add(i);
                }
            }

            if (Indices.Count < 3)
            {
                return;
            }

            bool clockwise = SignedArea(points, Indices) < 0f;
            int guard = Indices.Count * Indices.Count;

            while (Indices.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < Indices.Count; i++)
                {
                    int previousIndex = Indices[(i - 1 + Indices.Count) % Indices.Count];
                    int currentIndex = Indices[i];
                    int nextIndex = Indices[(i + 1) % Indices.Count];

                    Vector2 previous = points[previousIndex];
                    Vector2 current = points[currentIndex];
                    Vector2 next = points[nextIndex];
                    if (!IsConvex(previous, current, next, clockwise) || ContainsPoint(points, Indices, previousIndex, currentIndex, nextIndex, previous, current, next))
                    {
                        continue;
                    }

                    AddTriangle(vh, previous, current, next, colorEvaluator(previous, rect), colorEvaluator(current, rect), colorEvaluator(next, rect));
                    Indices.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (!clipped)
                {
                    AddFallbackFan(vh, rect, points, colorEvaluator);
                    return;
                }
            }

            if (Indices.Count == 3)
            {
                Vector2 a = points[Indices[0]];
                Vector2 b = points[Indices[1]];
                Vector2 c = points[Indices[2]];
                AddTriangle(vh, a, b, c, colorEvaluator(a, rect), colorEvaluator(b, rect), colorEvaluator(c, rect));
            }
        }

        public static void AddRoundedPolygonPath(IReadOnlyList<Vector2> points, IReadOnlyList<float> cornerRadii, float fallbackRadius, int cornerSegments, List<Vector2> output)
        {
            output.Clear();
            if (points == null || points.Count < 3)
            {
                return;
            }

            cornerSegments = Mathf.Max(2, cornerSegments);
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 previous = points[(i - 1 + points.Count) % points.Count];
                Vector2 current = points[i];
                Vector2 next = points[(i + 1) % points.Count];
                float radius = cornerRadii != null && i < cornerRadii.Count ? cornerRadii[i] : fallbackRadius;
                AddRoundedCorner(previous, current, next, Mathf.Max(0f, radius), cornerSegments, output);
            }
        }

        public static void AddPolyline(VertexHelper vh, IReadOnlyList<Vector2> points, float width, Color32 color, bool closed, LoogaUILineCap cap, LoogaUILineJoin join, int roundSegments)
        {
            AddPolyline(vh, points, width, color, closed, cap, join, roundSegments, false, 12f, 6f, 0f);
        }

        public static void AddPolyline(VertexHelper vh, IReadOnlyList<Vector2> points, float width, Color32 color, bool closed, LoogaUILineCap cap, LoogaUILineJoin join, int roundSegments, bool dashed, float dashLength, float gapLength, float dashOffset)
        {
            if (points == null || points.Count < 2 || width <= 0f)
            {
                return;
            }

            float halfWidth = width * 0.5f;
            int segmentCount = closed ? points.Count : points.Count - 1;
            float pathOffset = dashOffset;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                float length = Vector2.Distance(a, b);
                if (length <= 0.001f)
                {
                    continue;
                }

                if (dashed)
                {
                    AddDashedSegment(vh, a, b, width, color, cap, dashLength, gapLength, pathOffset);
                    pathOffset += length;
                }
                else
                {
                    AddLineSegment(vh, a, b, width, color, cap, !closed && i == 0, !closed && i == segmentCount - 1);
                }
            }

            if (!dashed)
            {
                AddLineCapsAndJoins(vh, points, halfWidth, color, closed, cap, join, roundSegments);
            }
        }

        public static Vector2 RectPoint(Rect rect, Vector2 normalizedPoint)
        {
            return new Vector2(rect.center.x + normalizedPoint.x * rect.width, rect.center.y + normalizedPoint.y * rect.height);
        }

        static void AddDashedSegment(VertexHelper vh, Vector2 start, Vector2 end, float width, Color32 color, LoogaUILineCap cap, float dashLength, float gapLength, float pathOffset)
        {
            float length = Vector2.Distance(start, end);
            if (length <= 0.001f)
            {
                return;
            }

            dashLength = Mathf.Max(0.001f, dashLength);
            gapLength = Mathf.Max(0f, gapLength);
            float patternLength = dashLength + gapLength;
            Vector2 direction = (end - start) / length;
            float distance = 0f;

            if (patternLength > 0f)
            {
                float offset = Mathf.Repeat(pathOffset, patternLength);
                distance = -offset;
            }

            while (distance < length)
            {
                float dashStart = Mathf.Max(distance, 0f);
                float dashEnd = Mathf.Min(distance + dashLength, length);
                if (dashEnd > dashStart)
                {
                    Vector2 dashA = start + direction * dashStart;
                    Vector2 dashB = start + direction * dashEnd;
                    AddLineSegment(vh, dashA, dashB, width, color, cap, true, true);
                    if (cap == LoogaUILineCap.Round)
                    {
                        AddCircle(vh, dashA, width * 0.5f, color, 12);
                        AddCircle(vh, dashB, width * 0.5f, color, 12);
                    }
                }

                distance += patternLength;
            }
        }

        static void AddLineSegment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color32 color, LoogaUILineCap cap, bool startCap, bool endCap)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude <= MinimumSqrDistance)
            {
                return;
            }

            direction.Normalize();
            float halfWidth = width * 0.5f;
            Vector2 normal = new(-direction.y, direction.x);
            Vector2 startExtension = cap == LoogaUILineCap.Square && startCap ? direction * halfWidth : Vector2.zero;
            Vector2 endExtension = cap == LoogaUILineCap.Square && endCap ? direction * halfWidth : Vector2.zero;
            Vector2 start = a - startExtension;
            Vector2 end = b + endExtension;
            AddQuad(vh, start + normal * halfWidth, end + normal * halfWidth, end - normal * halfWidth, start - normal * halfWidth, color);
        }

        static void AddLineCapsAndJoins(VertexHelper vh, IReadOnlyList<Vector2> points, float radius, Color32 color, bool closed, LoogaUILineCap cap, LoogaUILineJoin join, int roundSegments)
        {
            roundSegments = Mathf.Max(6, roundSegments);
            if (!closed && cap == LoogaUILineCap.Round)
            {
                AddCircle(vh, points[0], radius, color, roundSegments);
                AddCircle(vh, points[^1], radius, color, roundSegments);
            }

            for (int i = closed ? 0 : 1; i < (closed ? points.Count : points.Count - 1); i++)
            {
                if (join == LoogaUILineJoin.Round)
                {
                    AddCircle(vh, points[i], radius, color, roundSegments);
                }
                else
                {
                    AddBevelJoin(vh, points[(i - 1 + points.Count) % points.Count], points[i], points[(i + 1) % points.Count], radius, color);
                }
            }
        }

        static void AddBevelJoin(VertexHelper vh, Vector2 previous, Vector2 current, Vector2 next, float radius, Color32 color)
        {
            Vector2 previousDirection = (current - previous).normalized;
            Vector2 nextDirection = (next - current).normalized;
            if (previousDirection.sqrMagnitude <= MinimumSqrDistance || nextDirection.sqrMagnitude <= MinimumSqrDistance)
            {
                return;
            }

            Vector2 previousNormal = new(-previousDirection.y, previousDirection.x);
            Vector2 nextNormal = new(-nextDirection.y, nextDirection.x);
            AddTriangle(vh, current, current + previousNormal * radius, current + nextNormal * radius, color, color, color);
            AddTriangle(vh, current, current - previousNormal * radius, current - nextNormal * radius, color, color, color);
        }

        static void AddRoundedCorner(Vector2 previous, Vector2 current, Vector2 next, float radius, int segments, List<Vector2> output)
        {
            Vector2 toPrevious = previous - current;
            Vector2 toNext = next - current;
            float previousLength = toPrevious.magnitude;
            float nextLength = toNext.magnitude;
            if (radius <= 0.001f || previousLength <= 0.001f || nextLength <= 0.001f)
            {
                output.Add(current);
                return;
            }

            radius = Mathf.Min(radius, previousLength * 0.5f, nextLength * 0.5f);
            Vector2 start = current + toPrevious.normalized * radius;
            Vector2 end = current + toNext.normalized * radius;

            output.Add(start);
            for (int i = 1; i < segments; i++)
            {
                float t = i / (float)segments;
                Vector2 point = Vector2.Lerp(Vector2.Lerp(start, current, t), Vector2.Lerp(current, end, t), t);
                output.Add(point);
            }
            output.Add(end);
        }

        static void AddCircle(VertexHelper vh, Vector2 center, float radius, Color32 color, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float angleA = (360f / segments * i) * Mathf.Deg2Rad;
                float angleB = (360f / segments * (i + 1)) * Mathf.Deg2Rad;
                Vector2 a = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * radius;
                Vector2 b = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * radius;
                AddTriangle(vh, center, a, b, color, color, color);
            }
        }

        static void AddFallbackFan(VertexHelper vh, Rect rect, IReadOnlyList<Vector2> points, ShapeColorEvaluator colorEvaluator)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < points.Count; i++)
            {
                center += points[i];
            }

            center /= points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                AddTriangle(vh, center, a, b, colorEvaluator(center, rect), colorEvaluator(a, rect), colorEvaluator(b, rect));
            }
        }

        static float SignedArea(IReadOnlyList<Vector2> points, IReadOnlyList<int> indices)
        {
            float area = 0f;
            for (int i = 0; i < indices.Count; i++)
            {
                Vector2 a = points[indices[i]];
                Vector2 b = points[indices[(i + 1) % indices.Count]];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        static bool IsConvex(Vector2 previous, Vector2 current, Vector2 next, bool clockwise)
        {
            float cross = Cross(current - previous, next - current);
            return clockwise ? cross < 0f : cross > 0f;
        }

        static bool ContainsPoint(IReadOnlyList<Vector2> points, IReadOnlyList<int> indices, int previousIndex, int currentIndex, int nextIndex, Vector2 previous, Vector2 current, Vector2 next)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index == previousIndex || index == currentIndex || index == nextIndex)
                {
                    continue;
                }

                if (PointInTriangle(points[index], previous, current, next))
                {
                    return true;
                }
            }

            return false;
        }

        static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = Mathf.Abs(Cross(b - a, c - a));
            float areaA = Mathf.Abs(Cross(a - point, b - point));
            float areaB = Mathf.Abs(Cross(b - point, c - point));
            float areaC = Mathf.Abs(Cross(c - point, a - point));
            return Mathf.Abs(area - (areaA + areaB + areaC)) <= 0.01f;
        }

        static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }

    delegate Color32 ShapeColorEvaluator(Vector2 position, Rect rect);
}
