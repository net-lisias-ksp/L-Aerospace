# L Aerospace/KSP Division :: Known Issues

* `ModuleResourceIntakeController`
	+ On Editor, I'm failing to remove the "spurious" entries on PAW from the Controlled `PartModules`.
		- The thing works on Flight, so I think I missing the sweet spot to deactivate them when the thing is `OnStart`ed inside the Editor.	+ `controlGenerators = true` is not working - closing the intakes should shutdown all the generators, but this is not happening.
		- You need to shutdown the generators by hand after closing the intakes.
