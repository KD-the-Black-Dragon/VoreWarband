// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Do not delete Ironman saves before the independent ending record commits (spec 40E).
// 2026-11-06: Ordered background autosaves with detached capture, cancellation and nonblocking failure reporting.
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

internal sealed class CompanySaveWorker : MonoBehaviour
{
    internal sealed class Request
    {
        internal CompanyCampaign State;
        internal string Directory, Slot, Root;
        internal int Epoch;
        internal bool Manual;
        internal TaskCompletionSource<string> Completion;
        internal bool Publish(Action commit)
        {
            lock (Gate)
            {
                if ((Manual ? ManualVersion(State.Id) : Version(State.Id)) != Epoch) return false;
                commit(); return true;
            }
        }
    }
    static readonly object Gate = new object();
    internal static readonly object FileGate = new object();
    static readonly List<Request> Queue = new List<Request>();
    static readonly Dictionary<string, int> Epochs = new Dictionary<string, int>();
    static readonly Dictionary<string, int> ManualEpochs = new Dictionary<string, int>();
    static readonly Queue<string> Results = new Queue<string>();
#if UNITY_EDITOR
    internal static Action BeforeWrite;
#endif
    static Task worker;
    static CompanySaveWorker instance;
    Text notice;
    static int Version(string id) { int value; return Epochs.TryGetValue(id, out value) ? value : 0; }
    internal static bool Busy { get { lock (Gate) return worker != null && !worker.IsCompleted; } }
    static int ManualVersion(string id) { int value; return ManualEpochs.TryGetValue(id, out value) ? value : 0; }
    internal static void Cancel(string id, bool includeManual = false)
    {
        lock (Gate)
        {
            Epochs[id] = Version(id) + 1;
            if (includeManual) ManualEpochs[id] = ManualVersion(id) + 1;
            foreach (var request in Queue.Where(r => r.State.Id == id && (includeManual || !r.Manual)))
                if (request.Completion != null) request.Completion.TrySetCanceled();
            Queue.RemoveAll(r => r.State.Id == id && (includeManual || !r.Manual));
        }
    }
    internal static Task<string> Manual(CompanyCampaign state, string name)
    {
        if (state.Ironman || state.Ended) throw new InvalidOperationException("This company cannot create a manual save.");
        if (instance == null) instance = State.GameManager.gameObject.AddComponent<CompanySaveWorker>();
        name = (name ?? "").Trim();
        string slot = CompanySaveSlots.ManualSlot(name);
        Cancel(state.Id);
        var copy = CompanySnapshot.Take(state);
        copy.SaveSlot = slot; copy.SaveLabel = name; copy.SaveKind = 3;
        var request = new Request { State = copy, Directory = CompanySaveSlots.DirectoryFor(copy.Id), Root = CompanyCampaignSave.DefaultDirectory,
            Slot = slot, Manual = true, Completion = new TaskCompletionSource<string>() };
        lock (Gate)
        {
            request.Epoch = ManualVersion(copy.Id); Queue.Add(request);
            if (worker == null || worker.IsCompleted) worker = Task.Run((Action)Run);
        }
        return request.Completion.Task;
    }
    internal static void Enqueue(CompanyCampaign state, string directory)
    {
        if (state.Ended) return;
        if (instance == null) instance = State.GameManager.gameObject.AddComponent<CompanySaveWorker>();
        // This short main-thread copy preserves aliases/cycles. No encoding, validation,
        // disk access or worker wait happens on the player's action path.
        var copy = CompanySnapshot.Take(state);
        copy.SaveSlot = copy.InBattle ? "battle" : "map";
        copy.SaveLabel = copy.InBattle ? "Battle autosave" : "Map autosave";
        copy.SaveKind = copy.InBattle ? 2 : 1;
        lock (Gate)
        {
            Queue.RemoveAll(r => !r.Manual && r.State.Id == copy.Id && r.Slot == copy.SaveSlot);
            Queue.Add(new Request { State = copy, Directory = directory, Root = CompanyCampaignSave.DefaultDirectory, Slot = copy.SaveSlot, Epoch = Version(copy.Id) });
            if (worker == null || worker.IsCompleted) worker = Task.Run((Action)Run);
        }
    }
    static void Run()
    {
        while (true)
        {
            Request request;
            lock (Gate)
            {
                if (Queue.Count == 0) { worker = null; return; }
                request = Queue[0]; Queue.RemoveAt(0);
            }
            try
            {
#if UNITY_EDITOR
                if (BeforeWrite != null) BeforeWrite();
#endif
                string path = CompanySaveSlots.Write(request.State, request.Directory, request.Slot, request.State.SaveLabel, request.State.SaveKind, request);
                if (request.Completion != null)
                {
                    if (path == null) request.Completion.TrySetCanceled();
                    else request.Completion.TrySetResult(path);
                }
                if (path != null) lock (Gate) Results.Enqueue(null);
            }
            catch (Exception error)
            {
                if (request.Completion != null) request.Completion.TrySetException(error);
                lock (Gate) Results.Enqueue((request.Manual ? "Save failed. " : "Autosave failed. ") + error.Message + " You can keep playing and use Save / Load to retry.");
            }
        }
    }
    void Update()
    {
        lock (Gate)
        {
            while (Results.Count > 0)
            {
                string message = Results.Dequeue();
                if (notice == null && message != null)
                {
                    var canvas = new GameObject("Save notice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    canvas.transform.SetParent(transform, false);
                    canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.GetComponent<Canvas>().sortingOrder = 1500;
                    notice = UITheme.Label(canvas.transform, "", 16, 12, 1000, 52, 18);
                    notice.color = new Color(1, .65f, .5f); notice.gameObject.AddComponent<Outline>().effectColor = Color.black;
                }
                if (notice != null) notice.text = message ?? "";
            }
        }
    }
}
