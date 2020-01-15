using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

[CreateAssetMenu(fileName = "PotaTweenPreset", menuName = "PotaTween/Preset")]
public class PotaTweenPreset : ScriptableObject {

    private GameObject gameObject;
    private Transform transform;

    #region Pota Tween Values

    public string Tag;

    public bool PlayOnStart;
    public bool PlayOnEnable;

    [Header("Time")]
    public float Duration = 1;
    [Tooltip("Speed overwrite Duration")]
    public float Speed;
    public float Delay;

    [Header("Loop")]
    public LoopType Loop;
    public int LoopsNumber;

    [Header("Easing")]
    public EasingReference EasingReference;
    public Ease.Equation EaseEquation;
    public AnimationCurve Curve;

    [Tooltip("Reverses the curve when the loop is set to PingPong")]
    public bool FlipCurveOnReverse = true;

    [Header("Properties")]
    public PTTVector3 Position;
    public PTTVector3 Rotation;
    public PTTVector3 Scale;
    public PTTColor Color;
    public PTTFloat Alpha;
    public PTTFloat Float;

    [Header("Events")]
    public PotaTween.PotaTweenEvent onStart = new PotaTween.PotaTweenEvent();
    public PotaTween.PotaTweenEvent onComplete = new PotaTween.PotaTweenEvent();

    #endregion

    /** Creates a PotaTween component to play the preset */
    public PotaTween Initialize(GameObject gameObject, int id = 0)
    {
        this.gameObject = gameObject;
        this.transform = gameObject.transform;

        PotaTween tween = PotaTween.Create(this.gameObject, id);
        tween.Tag = Tag;
        tween.PlayOnEnable = PlayOnEnable;
        tween.PlayOnStart = PlayOnStart;
        tween.Duration = Duration;
        tween.Speed = Speed;
        tween.Delay = Delay;
        tween.Loop = Loop;
        tween.LoopsNumber = LoopsNumber;
        tween.EasingReference = EasingReference;
        tween.EaseEquation = EaseEquation;
        tween.Curve = Curve;
        tween.FlipCurveOnReverse = FlipCurveOnReverse;

        tween.Position = Position;
        tween.Rotation = Rotation;
        tween.Scale = Scale;
        tween.Color = Color;
        tween.Alpha = Alpha;
        tween.Float = Float;

        tween.onStart = onStart;
        tween.onComplete = onComplete;

        return tween;
    }
}
