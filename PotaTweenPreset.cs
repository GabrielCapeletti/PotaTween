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
    public void Initialize(GameObject gameObject, int id)
    {
        this.gameObject = gameObject;
        this.transform = gameObject.transform;

        PotaTween potaTween = PotaTween.Create(this.gameObject, id);
        potaTween.tag = Tag;
        potaTween.PlayOnEnable = PlayOnEnable;
        potaTween.PlayOnStart = PlayOnStart;
        potaTween.Duration = Duration;
        potaTween.Speed = Speed;
        potaTween.Delay = Delay;
        potaTween.Loop = Loop;
        potaTween.LoopsNumber = LoopsNumber;
        potaTween.EasingReference = EasingReference;
        potaTween.EaseEquation = EaseEquation;
        potaTween.Curve = Curve;
        potaTween.FlipCurveOnReverse = FlipCurveOnReverse;

        potaTween.Position = Position;
        potaTween.Rotation = Rotation;
        potaTween.Scale = Scale;
        potaTween.Color = Color;
        potaTween.Alpha = Alpha;
        potaTween.Float = Float;

        potaTween.onStart = onStart;
        potaTween.onComplete = onComplete;
    }
}
