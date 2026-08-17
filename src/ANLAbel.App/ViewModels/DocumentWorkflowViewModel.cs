using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using ANLAbel.Core.Workflow;
using ANLAbel.Data.Workflow;

namespace ANLAbel.App.ViewModels;
public sealed class DocumentWorkflowViewModel : INotifyPropertyChanged
{
    private readonly string _path; private readonly Func<string> _hash;
    private string _stateText="", _status="", _actor="local operator", _comment="";
    public DocumentWorkflowViewModel(string path, Func<string> hash) { _path=path; _hash=hash; Refresh(); }
    public event PropertyChangedEventHandler? PropertyChanged;
    public string StateText { get=>_stateText; private set=>Set(ref _stateText,value); }
    public string Status { get=>_status; private set=>Set(ref _status,value); }
    public string Actor { get=>_actor; set=>Set(ref _actor,value); }
    public string Comment { get=>_comment; set=>Set(ref _comment,value); }
    public IReadOnlyList<DocumentWorkflowTransition> Available { get; private set; } = Array.Empty<DocumentWorkflowTransition>();
    public void Refresh()
    {
        var store=DocumentWorkflowSidecar.Open(_path); var events=store.ReadValid(out var diagnostics); var hash=_hash(); var latest=events.LastOrDefault();
        var state=latest is not null && latest.DocumentHash==hash ? latest.To : DocumentWorkflowState.Draft;
        StateText=$"{state} · {hash[..Math.Min(12,hash.Length)]}";
        Status=diagnostics.Count>0 ? $"Audit requires repair: {string.Join(" ",diagnostics)}" : latest is not null && latest.DocumentHash!=hash ? "Document changed since the last workflow event; it is a new Draft revision." : "Local workflow audit is ready.";
        Available=diagnostics.Count==0 ? DocumentWorkflowContract.GetAvailable(state) : Array.Empty<DocumentWorkflowTransition>(); Changed(nameof(Available));
    }
    public bool Transition(DocumentWorkflowTransition transition)
    {
        try { var store=DocumentWorkflowSidecar.Open(_path); var hash=_hash(); var events=store.ReadValid(out var diagnostics); if(diagnostics.Count>0) throw new InvalidDataException("Workflow audit requires repair."); var from=events.LastOrDefault() is { } latest && latest.DocumentHash==hash ? latest.To : DocumentWorkflowState.Draft; store.Append(DocumentWorkflowSidecar.GetDocumentId(_path),hash,from,transition.To,Actor,Comment); Refresh(); return true; }
        catch(Exception ex) { Status=ex.Message; return false; }
    }
    private bool Set<T>(ref T f,T v,[CallerMemberName]string? n=null){if(EqualityComparer<T>.Default.Equals(f,v))return false;f=v;Changed(n);return true;} private void Changed([CallerMemberName]string? n=null)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(n));
}
