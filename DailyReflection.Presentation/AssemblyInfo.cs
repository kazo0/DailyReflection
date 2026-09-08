// The MVUX generator runs on this assembly (the models live here), so the bindable
// generation tool version has to be pinned here too — the head's copy only governs
// code generated into the head. v3 is the Uno.Extensions 7.2+ default: it improves
// Hot Reload and generates {Name}ViewModel instead of Bindable{Name}Model.
[assembly: Uno.Extensions.Reactive.Config.BindableGenerationTool(3)]
