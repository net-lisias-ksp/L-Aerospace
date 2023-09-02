# L Aerospace/KSP Division :: Known Issues

* `ModuleResourceIntakeController`
	+ On Editor, I'm failing to remove the "spurious" entries on PAW from the Controlled `PartModules`.
		- The thing works on Flight, so I think I missing the sweet spot to deactivate them when the thing is `OnStart`ed inside the Editor.
