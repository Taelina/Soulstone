using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Soulstone.Windows;

internal sealed class RollPresentationWindow : Window, IDisposable
{
    private static readonly Vector4 Gold = SoulstoneTheme.Gold;
    private static readonly Vector4 Muted = SoulstoneTheme.Muted;
    private static readonly Vector3[] DieVertices = CreateDieVertices();
    private static readonly (int A, int B, int C)[] DieFaces =
    {
        (0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11),
        (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
        (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9),
        (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)
    };
    private readonly Configuration configuration;
    private readonly ConcurrentQueue<DiceHistoryEntry> results = new();
    private readonly HashSet<string> presentedRequests = new(StringComparer.OrdinalIgnoreCase);
    private RollRequestPayload? request;
    private RollReveal? reveal;
    private bool requestFailed;

    public RollPresentationWindow(Configuration configuration)
        : base("###SoulstoneRollPresentation", ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.configuration = configuration;
        Size = new Vector2(380, 440);
        SizeCondition = ImGuiCond.Always;
        ShowCloseButton = false;
        RespectCloseHotkey = true;
        ForceMainWindow = true;
        AllowPinning = false;
        AllowClickthrough = false;
        AllowBackgroundBlur = false;
        DiceHistoryManager.Instance.OnEntryAdded += OnEntryAdded;
    }

    private void OnEntryAdded(DiceHistoryEntry entry)
    {
        if (entry.IsLocal)
            results.Enqueue(entry);
    }

    // Called on the UI thread. Network callbacks only enqueue data.
    public void ProcessPending()
    {
        if (!configuration.ShowRollPresentation)
        {
            Reset();
            return;
        }

        var pending = PartySyncManager.Instance.PendingRollRequests;
        presentedRequests.RemoveWhere(id => !pending.ContainsKey(id));
        if (!IsOpen)
        {
            request = null;
            reveal = null;
            requestFailed = false;
        }
        if (request != null && !pending.ContainsKey(request.RequestId))
        {
            request = null;
            IsOpen = false;
        }

        if (request != null || reveal != null || requestFailed) return;

        if (results.TryDequeue(out var result))
        {
            reveal = new RollReveal(result, ImGui.GetTime());
            IsOpen = true;
            return;
        }

        var nextRequest = pending.Values.FirstOrDefault(r => !presentedRequests.Contains(r.RequestId));
        if (nextRequest != null) OpenRequest(nextRequest);
    }

    public void OpenRequest(RollRequestPayload pendingRequest)
    {
        if (!PartySyncManager.Instance.PendingRollRequests.ContainsKey(pendingRequest.RequestId)) return;
        presentedRequests.Add(pendingRequest.RequestId);
        request = pendingRequest;
        reveal = null;
        requestFailed = false;
        IsOpen = true;
    }

    public void Reset()
    {
        results.Clear();
        presentedRequests.Clear();
        request = null;
        reveal = null;
        requestFailed = false;
        IsOpen = false;
    }

    public void Dispose()
    {
        DiceHistoryManager.Instance.OnEntryAdded -= OnEntryAdded;
        Reset();
    }

    private static string Loc(string key) => LocalizationManager.Instance.GetLocalizedString(key);

    public override void PreDraw()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos + viewport.Size / 2, ImGuiCond.Always, new Vector2(0.5f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(16, 12) * ImGuiHelpers.GlobalScale);
    }

    public override void PostDraw() => ImGui.PopStyleVar(2);

    private static void CenteredText(string text, Vector4 color)
    {
        float offset = Math.Max(0, (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) * 0.5f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        var position = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();
        draw.AddText(position + Vector2.One * ImGuiHelpers.GlobalScale, 0xDD000000, text);
        draw.AddText(position, ImGui.ColorConvertFloat4ToU32(color), text);
        ImGui.Dummy(ImGui.CalcTextSize(text));
    }

    public override void Draw()
    {
        var result = reveal?.Result;
        double now = ImGui.GetTime();
        if (reveal?.ShouldClose(now) == true)
        {
            ClosePresentation();
            return;
        }
        bool revealed = reveal?.IsRevealed(now) == true;
        ImGui.Spacing();
        CenteredText(request?.RollName ?? result?.RollName ?? string.Empty, Vector4.One);
        if (request != null)
        {
            CenteredText(request.Formula, Gold);
        }
        ImGui.Spacing();

        var accent = revealed && result != null
            ? result.IsCriticalSuccess ? new Vector4(0.4f, 0.85f, 0.55f, 1f)
            : result.IsCriticalFailure ? new Vector4(0.95f, 0.35f, 0.32f, 1f) : Gold
            : Gold;
        bool clicked = DrawDie(revealed ? result!.Total.ToString() : request != null ? "?" : "…",
            accent, reveal?.Progress(now) ?? 1f, reveal?.RevealElapsed(now) ?? 0, now, request != null);
        if (ImGui.IsItemHovered() && (request != null || revealed))
        {
            using var tooltip = ImRaii.Tooltip();
            if (request != null)
            {
                ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("RollStageRequestedBy", request.RequestedBy));
            }
            else if (result != null)
            {
                ImGui.TextUnformatted(result.ResultDisplay);
                if (!string.IsNullOrWhiteSpace(result.Details))
                    ImGui.TextUnformatted(LocalizationManager.Instance.GetLocalizedString("RollStageDiceResults", result.Details));
            }
        }

        if (request != null)
        {
            bool advantage = request.Advantage && !request.Disadvantage;
            bool disadvantage = request.Disadvantage && !request.Advantage;
            if (advantage || disadvantage)
                CenteredText(Loc(advantage ? "BadgeAdvantage" : "BadgeDisadvantage"), Muted);
            CenteredText(Loc(request.IsPrivate ? "RollPrivateTag" : "RollPublicTag"), Gold);
            ImGui.Spacing();
            CenteredText(Loc("RollStageReady"), Gold);
            if (clicked)
            {
                // The manager consumes the request atomically and broadcasts exactly once.
                requestFailed = !PartySyncManager.Instance.ExecuteRollRequest(request.RequestId);
                request = null;
                if (!requestFailed)
                {
                    IsOpen = false;
                    ProcessPending();
                }
            }
            if (request != null && TextButton("GroupDismissRoll"))
            {
                PartySyncManager.Instance.DismissRollRequest(request.RequestId);
                request = null;
                IsOpen = false;
            }
        }
        else if (result != null)
        {
            CenteredText(Loc(revealed
                ? result.IsCriticalSuccess ? "RollStageCriticalSuccess"
                : result.IsCriticalFailure ? "RollStageCriticalFailure" : "RollStageTotal"
                : "RollStageRolling"), accent);
            CenteredText(Loc(result.IsPrivate ? "RollPrivateTag" : "RollPublicTag"), Muted);
            ImGui.Spacing();
            CenteredText(Loc(revealed ? "RollStageClickClose" : "RollStageClickSkip"), Muted);
            if (clicked)
            {
                if (revealed)
                    ClosePresentation();
                else
                    reveal!.Skip(now);
            }
        }

        if (requestFailed)
        {
            ImGui.TextWrapped(Loc("RollStageRequestFailed"));
            if (TextButton("RollStageContinue"))
            {
                requestFailed = false;
                IsOpen = false;
            }
        }
    }

    private void ClosePresentation()
    {
        reveal = null;
        request = null;
        requestFailed = false;
        IsOpen = false;
    }

    private static bool TextButton(string key)
    {
        float scale = ImGuiHelpers.GlobalScale;
        float width = Math.Min(240 * scale, ImGui.GetContentRegionAvail().X);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, (ImGui.GetContentRegionAvail().X - width) / 2));
        using var button = ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero);
        using var hover = ImRaii.PushColor(ImGuiCol.ButtonHovered, new Vector4(0.83f, 0.67f, 0.37f, 0.1f));
        using var active = ImRaii.PushColor(ImGuiCol.ButtonActive, new Vector4(0.83f, 0.67f, 0.37f, 0.2f));
        using var text = ImRaii.PushColor(ImGuiCol.Text, Muted);
        return ImGui.Button(Loc(key), new Vector2(width, 32 * scale));
    }

    private static bool DrawDie(string value, Vector4 accent, float progress, double revealElapsed, double now, bool ready)
    {
        float scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        float width = ImGui.GetContentRegionAvail().X;
        var restCenter = origin + new Vector2(width / 2, 130 * scale);
        float remaining = 1 - progress;
        float spin = 1 - remaining * remaining * remaining;
        float bounce = Math.Abs(MathF.Sin(progress * MathF.PI * 4)) * remaining;
        float landing = ready ? 0 : MathF.Sin((float)revealElapsed * 18) * MathF.Exp(-(float)revealElapsed * 8);
        var center = restCenter - new Vector2(0, (bounce * 38 - landing * 5) * scale);
        if (ready) center.Y += MathF.Sin((float)now * 1.8f) * 3 * scale;
        float radius = (78 + landing * 5) * scale;
        float squash = 1 + MathF.Sin(progress * MathF.PI * 10) * remaining * 0.08f;
        var rotation = Quaternion.CreateFromYawPitchRoll(
            0.25f + spin * MathF.Tau * 3,
            -0.2f + spin * MathF.Tau * 2,
            0.12f + MathF.Sin(progress * MathF.PI * 6) * remaining * 0.45f);
        var draw = ImGui.GetWindowDrawList();
        uint gold = ImGui.ColorConvertFloat4ToU32(accent);

        // A soft halo and a ground shadow keep the floating die readable over the game.
        for (int i = 5; i > 0; i--)
            draw.AddCircleFilled(center, radius + (8 + i * 5) * scale, Tint(accent, 0.018f), 64);
        for (int i = 0; i < 32; i++)
        {
            float a = i * MathF.Tau / 32;
            draw.PathLineTo(restCenter + new Vector2(MathF.Cos(a) * (55 - bounce * 15), 85 + MathF.Sin(a) * 9) * scale);
        }
        draw.PathFillConvex(0x44000000);

        float orbit = ready ? (float)now * 0.35f : spin * MathF.Tau;
        draw.AddCircle(center, radius + 30 * scale, Tint(accent, 0.16f), 64, scale);
        for (int i = 0; i < 18; i++)
        {
            float a = i * MathF.Tau / 18 + orbit;
            float burst = !ready && progress >= 1 ? Math.Clamp((float)revealElapsed / 0.65f, 0, 1) : 0;
            float distance = radius + (22 + MathF.Sin(a * 3) * 8 + burst * 48) * scale;
            var position = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * distance;
            float alpha = !ready && progress >= 1 ? (1 - burst) * 0.85f : 0.25f + remaining * 0.5f;
            draw.AddCircleFilled(position, (i % 3 == 0 ? 2.2f : 1.2f) * scale, Tint(accent, alpha), 8);
            if (progress < 1)
                draw.AddLine(position, position - new Vector2(-MathF.Sin(a), MathF.Cos(a)) * 10 * remaining * scale, Tint(accent, alpha * 0.4f), scale);
        }
        if (!ready && progress >= 1 && revealElapsed < 0.65)
        {
            float burst = (float)revealElapsed / 0.65f;
            draw.AddCircle(center, radius + (10 + burst * 70) * scale, Tint(accent, (1 - burst) * 0.7f), 64, 2 * scale);
        }

        // Project an icosahedron into the ImGui draw list; no textures or 3D renderer needed.
        Span<Vector3> vertices = stackalloc Vector3[12];
        Span<Vector2> projected = stackalloc Vector2[12];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Transform(DieVertices[i], rotation);
            float perspective = 4 / (4 - vertices[i].Z);
            projected[i] = center + new Vector2(vertices[i].X / squash, -vertices[i].Y * squash) * radius * perspective;
        }

        Span<(int Index, float Depth)> faces = stackalloc (int, float)[20];
        int faceCount = 0;
        for (int i = 0; i < DieFaces.Length; i++)
        {
            var (a, b, c) = DieFaces[i];
            var normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            var midpoint = (vertices[a] + vertices[b] + vertices[c]) / 3;
            if (Vector3.Dot(normal, new Vector3(0, 0, 4) - midpoint) <= 0) continue;
            var face = (Index: i, Depth: midpoint.Z);
            int insert = faceCount++;
            while (insert > 0 && faces[insert - 1].Depth > face.Depth)
            {
                faces[insert] = faces[insert - 1];
                insert--;
            }
            faces[insert] = face;
        }
        var light = Vector3.Normalize(new Vector3(-0.5f, 0.8f, 1));
        for (int i = 0; i < faceCount; i++)
        {
            var (a, b, c) = DieFaces[faces[i].Index];
            var normal = Vector3.Normalize(Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]));
            float brightness = Math.Clamp(Vector3.Dot(normal, light), 0, 1);
            var faceColor = new Vector4(0.055f + brightness * 0.14f, 0.09f + brightness * 0.19f, 0.16f + brightness * 0.28f, 1);
            draw.AddTriangleFilled(projected[a], projected[b], projected[c], ImGui.ColorConvertFloat4ToU32(faceColor));
            draw.AddTriangle(projected[a], projected[b], projected[c], Tint(accent, 0.35f + brightness * 0.6f), 1.2f * scale);
        }

        var font = ImGui.GetFont();
        float textScale = Math.Min(2.5f, 100 * scale / Math.Max(1, ImGui.CalcTextSize(value).X));
        float fontSize = ImGui.GetFontSize() * textScale;
        var textSize = ImGui.CalcTextSize(value) * textScale;
        var textPos = center - textSize / 2;
        draw.AddText(font, fontSize, textPos + new Vector2(2, 2) * scale, 0xFF000000, value);
        draw.AddText(font, fontSize, textPos, gold, value);
        ImGui.SetCursorScreenPos(origin + new Vector2(Math.Max(0, (width - 260 * scale) / 2), 0));
        return ImGui.InvisibleButton("##ThrowDie", new Vector2(Math.Min(width, 260 * scale), 260 * scale));
    }

    private static uint Tint(Vector4 color, float alpha) => ImGui.ColorConvertFloat4ToU32(new Vector4(color.X, color.Y, color.Z, alpha));

    private static Vector3[] CreateDieVertices()
    {
        float phi = (1 + MathF.Sqrt(5)) / 2;
        Vector3[] vertices =
        {
            new(-1, phi, 0), new(1, phi, 0), new(-1, -phi, 0), new(1, -phi, 0),
            new(0, -1, phi), new(0, 1, phi), new(0, -1, -phi), new(0, 1, -phi),
            new(phi, 0, -1), new(phi, 0, 1), new(-phi, 0, -1), new(-phi, 0, 1)
        };
        for (int i = 0; i < vertices.Length; i++) vertices[i] = Vector3.Normalize(vertices[i]);
        return vertices;
    }
}
