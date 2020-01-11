# PotaTween

## Installation

You have two options

1. download and extract the zip inside your Assets folder
2. in your terminal type `git clone https://github.com/GabrielCapeletti/PotaTween.git`
   > This will automatically create a folder called PotaTween

## Getting Started

#### Animation from Editor

<img align="right" width="150" height="100" style="object-fit: cover" src="https://user-images.githubusercontent.com/20288761/72187772-55e0c300-33d7-11ea-9c31-24a97bbcdbde.png">

1. Add `PotaTween` Component to the GameObject you want to animate.
2. In the Inspector set the values as the image ->
3. Done! When you play the game the animation will automatically start!

#### Animation from Code

1. From any piece of code, where you want to play the animation, just type

```csharp
PotaTween tween = PotaTween.Create(gameObject, 0);
tween.SetScale(Vector3.zero, Vector3.one);                      // Animates from scale 0 to 1
tween.Play();
```

You can add a parameter to the method `Play(here)` to dispach a callback on finish animation.
