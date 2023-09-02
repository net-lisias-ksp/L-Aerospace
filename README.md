# L Aerospace / KSP Division

A bag of tricks and tools for my Part Sets, providing unusual features for unusual needs.


## About L Aerospace

A new kid on the block, plain simple. We came from nowhere and we don't know where we're going, but hey, we know how to fly to there fast!

We still have some landing surviving issues, but we're working on that.

**L Aerospace KSP Division: Boldly crashing what no Kerbal has crashed before.**


## Description

### `ModuleResourceIntakeController`

A `PartModule` to consolidate the control of many `ModuleResourceIntake` and `ModuleGenerators` on a single part.

All the Intakes will be controlled at once using the PAW. All the `ModuleResourceIntake` PAW widgets, Actions and KSPEvents are hijacked by this module.

The Generators may or may not be controlled with the Intakes. The Generator's PAW widgets, Actions and KSPEvents **are not** hijacked.

Add the following section into your Part config to use it:

```
        MODULE
        {
                name = ModuleResourceIntakeController
                intakesEnabled = true
                controlGenerators = true
        }
```

* `intakesEnabled` tells the current state of the Intakes, and this setting will be used to reconfigure the `ModuleResourceIntake` on start disregarding the current target settings.
* `controlGenerators` configures if any `ModuleGenerator` on the part will be shutdown when the Intake is closed, and activated if the Intake is opened (if and only if the Generator was activated when the intake was closed).

You can add any number of `ModuleResourceIntake` and `ModuleGenerator` you want into the part, but only the first 4 Intakes will have PAW status displayed.

Ideally `ModuleResourceIntakeController` should be installed **before** any controlled `PartModules` to avoid flickering on the PAW.

This `PartModule` can't be used together:

* `ModulePartVariants`
	+ I need to add proper Variant support on this thing, not to mention that it may try to control the `PartModule`s itself.
* Any Fuel Switches
	+ Fuel Switches know nothing about `ModuleResourceIntakeController` and no study was made to check for misbehaviours. Yet.
	+ B9PS, **definitively** should not be used with `ModuleResourceIntakeController` as it may try to rewire the `PartModules` itself.


## License

This work is double licensed under [SKL 1.0](http://ksp.lisias.net/SKL-1_0.txt) and [GPL 2.0](https://www.gnu.org/licenses/old-licenses/gpl-2.0.txt) - at your discretion and/or need.

- - - 

See also:

* L Aerospace KSP Division
	+ [Home Page](http://ksp.lisias.net/)
	+ [Research & Development Headquarters](https://github.com/net-lisias-ksp)
