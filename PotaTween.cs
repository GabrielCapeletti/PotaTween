using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/**
   11/07 - added compatibility to alpha and color animations for UI.Text, TextMeshPro, TextMeshProUGUI 
   25/07 - refactored finishCallback to be a list of Actions, allowing for multiple callbacks to be set per Tween
 */

#region Classes
[Serializable]
public class PTTVector3
{
	public Vector3 From, To;
	public bool IsLocal, IsRelative;
	public PTTAxisMask IgnoreAxis;
	
	public PTTVector3(Vector3 value)
	{
		this.From = value;
		this.To = value;
	}
	
	public PTTVector3(Vector3 from, Vector3 to)
	{
		this.From = from;
		this.To = to;
	}
	
	public PTTVector3(Vector3 from, Vector3 to, bool isLocal)
	{
		this.From = from;
		this.To = to;
		this.IsLocal = isLocal;
	}
	
	public PTTVector3(Vector3 from, Vector3 to, bool isLocal, bool isRelative)
	{
		this.From = from;
		this.To = to;
		this.IsLocal = isLocal;
		this.IsRelative = isRelative;
	}
	
	public PTTVector3(Vector3 from, Vector3 to, bool isLocal, bool isRelative, PTTAxisMask ignoreAxis)
	{
		this.From = from;
		this.To = to;
		this.IsLocal = isLocal;
		this.IsRelative = isRelative;
		this.IgnoreAxis = ignoreAxis;
	}
}

[Serializable]
public class PTTFloat
{
	public float From, To;
	[HideInInspector]
	public float Value;
	
	public PTTFloat(float value)
	{
		this.From = value;
		this.To = value;
	}
	
	public PTTFloat(float from, float to)
	{
		this.From = from;
		this.To = to;
	}
}

[Serializable]
public class PTTColor
{
	public Color From, To;
	
	public PTTColor(Color value)
	{
		this.From = value;
		this.To = value;
	}
	
	public PTTColor(Color from, Color to)
	{
		this.From = from;
		this.To = to;
	}
}

public class PTTTimedAction
{
	public Action Action;
	public float Time;
	
	public PTTTimedAction(Action action, float time)
	{
		this.Action = action;
		this.Time = time;
	}
}

[Serializable]
public struct PTTAxisMask
{
	public bool X, Y, Z;
	
	public bool All
	{
		get { return (X && Y && Z); }
		set { X = value; Y = value; Z = value; }
	}
	
	public bool None
	{
		get { return (!X && !Y && !Z); }
	}
	
	public PTTAxisMask(bool x, bool y, bool z)
	{
		this.X = x;
		this.Y = y;
		this.Z = z;
	}
}

#endregion

#region Enums
public enum LoopType
{
	None,
	Loop,
	PingPong
}

public enum TweenAxis
{
	X, Y, Z
}

public enum TweenType
{
	From, To
}

public enum EasingReference
{
	Equation,
	Curve
}
#endregion

public class PotaTween : MonoBehaviour
{
    [Serializable]
    public class PotaTweenEvent : UnityEvent<PotaTween>
    {
        
    }

    public string Tag;
	public int Id;

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
	
	private bool _hasReverted = false;
	
	private Rigidbody _rigidbody;
	private Rigidbody2D _rigidbody2D;
	
	private Vector3 _startPosition, _startRotation, _startScale;
	
	#region Properties
	public float ElapsedDelay { get; set; }
	public float ElapsedTime { get; set; }

	[Header("Debug")]
	[SerializeField]
	protected bool _isPlaying;
	public bool IsPlaying
	{
		get { return _isPlaying; }
	}
	
	[SerializeField]
	protected bool _isReversing;
	public bool IsReversing
	{ 
		get { return _isReversing; }
	}

	[SerializeField]
	protected bool _isPaused;
	public bool IsPaused
	{
		get { return _isPaused; }
	}
	
	protected bool _hasCompleted;
	public bool HasCompleted
	{
		get { return _hasCompleted; }
	}
	
	private int _loopcount;
	public int LoopCount
	{
		get { return _loopcount; }
	}
	
	public GameObject Target
	{
		get { return gameObject; }
	}

	private Transform _ownTransform;
	public Transform OwnTransform
	{
		get
		{
			if (_ownTransform == null)
				_ownTransform = transform;
			return _ownTransform;
		}
	}
	#endregion

	[Header("Events")]
	[SerializeField]
	protected PotaTweenEvent _onStart = new PotaTweenEvent();
	public PotaTweenEvent onStart
	{
		get { return _onStart; }
		set { _onStart = value; }
	}
	
	[SerializeField]
	protected PotaTweenEvent _onComplete = new PotaTweenEvent();
	public PotaTweenEvent onComplete
	{
		get { return _onComplete; }
		set { _onComplete = value; }
	}

	protected void Awake()
	{
		if ((Curve == null || Curve.keys.Length <= 1) && EasingReference != EasingReference.Equation)
		{
			EasingReference = EasingReference.Equation;
			EaseEquation = Ease.Equation.Linear;
		}

		easeMethod = System.Type.GetType("Ease").GetMethod(EaseEquation.ToString());
		
		_colorProperty = "_Color";
		GetRenderers();
        
		_rigidbody = GetComponent<Rigidbody>();
		_rigidbody2D = GetComponent<Rigidbody2D>();

		_startPosition = OwnTransform.position;
		_startRotation = OwnTransform.eulerAngles;
		_startScale = OwnTransform.localScale;
	}
	
	protected void Start()
	{
		if (PlayOnStart)
			Play();
	}
	
	protected void OnEnable()
	{		
		if (PlayOnEnable)
		{
			if (!_hasReverted)
				Play();
			else
				Reverse();
		}
	}

	protected void OnDisable()
	{
		Stop();
	}

	protected void Update()
	{
		if (!IsPlaying || IsPaused)
			return;
		
		if (DelayLoop())
			return;
		
		if (TweenLoop())
			return;
		
		TweenCompleted();
	}
	
	#region Create
	/// <summary>
	/// Creates a new PotaTween, or returns an existing with the same target and id.
	/// </summary>
	/// <param name="target">GameObject that the tween will be attached to.</param>
	/// <param name="id">Id of the tween for reference.</param>
	public static PotaTween Create(GameObject target, int id = 0)
	{
		PotaTween[] tweens = target.GetComponents<PotaTween>();
		
		if (tweens != null)
		{
			for (int i = 0; i < tweens.Length; i++)
			{
				if (tweens[i].Id == id)
				{
					return tweens[i];
				}
			}
		}
		
		PotaTween tween = target.AddComponent<PotaTween>();

		tween.Id = id;
		tween.Initialize();
		
		return tween;
	}	
	#endregion
	
	#region Setters
	#region Position
	public PotaTween SetPosition(Vector3 from, Vector3 to, bool isLocal = false, bool isRelative = false)
	{
		Position = new PTTVector3(from, to, isLocal, isRelative);
		Position.IgnoreAxis = new PTTAxisMask(false, false, false);
		
		return this;
	}
	
	public PotaTween SetPosition(TweenAxis axis, float from, float to, bool isLocal = false, bool isRelative = false)
	{
		if (Position.IgnoreAxis.None)
			Position.IgnoreAxis = new PTTAxisMask(true, true, true);
		
		switch (axis)
		{
		case TweenAxis.X:
			Position.IgnoreAxis.X = false;
			Position.From.x = from;
			Position.To.x = to;
			break;
			
		case TweenAxis.Y:
			Position.IgnoreAxis.Y = false;
			Position.From.y = from;
			Position.To.y = to;
			break;
			
		case TweenAxis.Z:
			Position.IgnoreAxis.Z = false;
			Position.From.z = from;
			Position.To.z = to;
			break;
		}
		
		Position.IsLocal = isLocal;
		Position.IsRelative = isRelative;
		
		return this;
	}
	#endregion
	
	#region Rotation
	public PotaTween SetRotation(Vector3 from, Vector3 to, bool isLocal = false, bool isRelative = false)
	{

		Rotation = new PTTVector3(from, to, isLocal, isRelative);

		Rotation.IgnoreAxis = new PTTAxisMask(false, false, false);
		
		return this;
	}
	
	public PotaTween SetRotation(TweenAxis axis, float from, float to, bool isLocal = false, bool isRelative = false)
	{
		if (Rotation.IgnoreAxis.None)
			Rotation.IgnoreAxis = new PTTAxisMask(true, true, true);
		
		switch (axis)
		{
		case TweenAxis.X:
			Rotation.IgnoreAxis.X = false;
			Rotation.From.x = from;
			Rotation.To.x = to;
			break;
			
		case TweenAxis.Y:
			Rotation.IgnoreAxis.Y = false;
			Rotation.From.y = from;
			Rotation.To.y = to;
			break;
			
		case TweenAxis.Z:
			Rotation.IgnoreAxis.Z = false;
			Rotation.From.z = from;
			Rotation.To.z = to;
			break;
		}
		
		Rotation.IsLocal = isLocal;
		Rotation.IsRelative = isRelative;
		
		return this;
	}
	#endregion
	
	#region Scale
	public PotaTween SetScale(Vector3 from, Vector3 to, bool isRelative = false)
	{
		Scale = new PTTVector3(from, to, false, isRelative);
		Scale.IgnoreAxis = new PTTAxisMask(false, false, false);
		
		return this;
	}
	
	public PotaTween SetScale(TweenAxis axis, float from, float to, bool isRelative = false)
	{
		if (Scale.IgnoreAxis.None)
			Scale.IgnoreAxis = new PTTAxisMask(true, true, true);
		
		switch (axis)
		{
		case TweenAxis.X:
			Scale.IgnoreAxis.X = false;
			Scale.From.x = from;
			Scale.To.x = to;
			break;
			
		case TweenAxis.Y:
			Scale.IgnoreAxis.Y = false;
			Scale.From.y = from;
			Scale.To.y = to;
			break;
			
		case TweenAxis.Z:
			Scale.IgnoreAxis.Z = false;
			Scale.From.z = from;
			Scale.To.z = to;
			break;
		}
		
		Scale.IsRelative = isRelative;
		
		return this;
	}
	#endregion
	
	#region Color
	private Renderer[] _renderers;
	private UnityEngine.UI.Image[] _images;
	private UnityEngine.UI.Text[] _texts;
	private TMPro.TextMeshPro[] _textsPro;
	private TMPro.TextMeshProUGUI[] _textsProUGUI;
	private Material[] _bkpMaterials;
	private string _colorProperty;
	
	#region Color
	public PotaTween SetColor(Color from, Color to, string colorProperty = "_Color")
	{
		Color = new PTTColor(from, to);
		this._colorProperty = colorProperty;
		GetRenderers();

		return this;
	}
	#endregion
	
	#region Alpha
	public PotaTween SetAlpha(float from, float to, string colorProperty = "_Color")
	{
		Alpha = new PTTFloat(from, to);
		this._colorProperty = colorProperty;
		GetRenderers();

		return this;
	}
	
	public PotaTween SetAlpha(TweenType type, float value, string colorProperty = "_Color")
	{
		this._colorProperty = colorProperty;
		
		Renderer tempRender = GetComponent<Renderer>() ? GetComponent<Renderer>() : GetComponentInChildren<Renderer>();
		Image tempImageRender = GetComponentInChildren<Image>();
		
		float targetAlpha = tempImageRender != null ? tempImageRender.color.a : tempRender.sharedMaterial.GetColor(colorProperty).a;
		if (tempRender is SpriteRenderer)
		{
			targetAlpha = ((SpriteRenderer)tempRender).color.a;
		}
		
		switch (type)
		{
		case TweenType.From:
			Alpha = new PTTFloat(value, targetAlpha);
			break;
			
		case TweenType.To:
			Alpha = new PTTFloat(targetAlpha, value);
			break;
		}
		
		GetRenderers();
		return this;
	}
	#endregion
	
	private void GetRenderers()
	{
		_renderers = GetComponentsInChildren<Renderer>(true);
		
		_images = GetComponentsInChildren<UnityEngine.UI.Image>(true);
		
		_texts = GetComponentsInChildren<UnityEngine.UI.Text>(true);
		_textsPro = GetComponentsInChildren<TextMeshPro>(true);
		_textsProUGUI = GetComponentsInChildren<TextMeshProUGUI>(true);
		
		_bkpMaterials = new Material[_renderers.Length];
		
		for (int i = 0; i < _bkpMaterials.Length; i++)
		{
			_bkpMaterials[i] = _renderers[i].sharedMaterial;
		}
	}
	#endregion
	
	#region Float
	public PotaTween SetFloat(float from, float to)
	{
		Float = new PTTFloat(from, to);
		
		return this;
	}

	public PotaTween SetFloat(float from, float to, Action updateCallback)
	{
		Float = new PTTFloat(from, to);
		this._updateCallback = updateCallback;
		
		return this;
	}
	#endregion
	
	#region Duration
	public PotaTween SetDuration(float time)
	{
		Duration = time;
		return this;
	}
	public void SetDurationEvent(float time)
	{
		Duration = time;
	}
	#endregion
	
	#region Speed
	public PotaTween SetSpeed(float speed)
	{
		this.Speed = speed;
		
		CalculateDuration();
		
		return this;
	}
	public void SetSpeedEvent(float speed)
	{
		this.Speed = speed;
		
		CalculateDuration();
	}
	#endregion
	
	#region Delay
	public PotaTween SetDelay(float time)
	{
		Delay = time;
		return this;
	}
	
	public void SetDelayEvent(float time)
	{
		Delay = time;
	}
	#endregion
	
	#region Loop
	public PotaTween SetLoop(LoopType loop)
	{
		Loop = loop;
		return this;
	}
	
	public PotaTween SetLoop(LoopType loop, int loopsNumber)
	{
		Loop = loop;
		LoopsNumber = loopsNumber;
		return this;
	}
	#endregion
	
	#region Ease
	float _easeValue;
	System.Reflection.MethodInfo easeMethod = null;
	public PotaTween SetEaseEquation(Ease.Equation easeEquation)
	{
		EaseEquation = easeEquation;
		EasingReference = EasingReference.Equation;
		easeMethod = System.Type.GetType("Ease").GetMethod(EaseEquation.ToString());
		return this;
	}
	#endregion
	
	#region Curve
	public PotaTween SetCurve(AnimationCurve curve)
	{
		Curve = curve;
		EasingReference = EasingReference.Curve;
		return this;
	}
	#endregion
	
	#region Callbacks
	Action _startCallback, _updateCallback;
	private List<Action<GameObject>> finishCallback = new List<Action<GameObject>>();
	public PotaTween StartCallback(Action callback)
	{
		_startCallback = callback;
		return this;
	}
	
	public PotaTween UpdateCallback(Action callback)
	{
		_updateCallback = callback;
		return this;
	}
	
	public PotaTween AddFinishCallback(Action<GameObject> callback)
	{
		finishCallback.Add(callback);
		return this;
	}
    public PotaTween RemoveFinishCallback(Action<GameObject> callback)
    {
        finishCallback.Remove(callback);
        return this;
    }
    public PotaTween ClearFinishCallback(Action<GameObject> callback)
    {
        finishCallback.Clear();
        return this;
    }

    List<PTTTimedAction> timedCallbacks;
	public PotaTween TimedCallback(Action callback, float approxTime)
	{
		if (timedCallbacks == null)
			timedCallbacks = new List<PTTTimedAction>();
		
		timedCallbacks.Add(new PTTTimedAction(callback, approxTime));
		return this;
	}
	
	public PotaTween RemoveAllTimedCallbacks()
	{
		timedCallbacks = new List<PTTTimedAction>();
		return this;
	}
	#endregion
	
	#endregion
	
	#region Functions
	Action callback;
	/// <summary>
	/// Plays the animation.
	/// </summary>
	public void Play()
	{
		Play(null);
	}
	/// <summary>
	/// Plays the animation
	/// </summary>
	/// <param name="callback">The callback function to be called at the end of the animation</param>
	public void Play(Action callback)
	{
		if (!Target.activeSelf) return;
		
		this.callback = callback;
		
		_isPaused = false;
		
		if (!IsPlaying)
		{
			_isPlaying = true;
			_isReversing = false;
			onStart.Invoke(this);
			
			if (_hasReverted)
			{
				ReverseDirection();
			}
			_loopcount = 0;
			
			StartTween();
		}
	}

	/// <summary>
	/// Plays the animation with the specified tag.
	/// </summary>
	/// <param name="tag">Tag of the animation.</param>
	public void PlayWithTag(string tag)
	{
		if (Tag == tag)
			Play();
		else
			GetTweenWithTag(tag).Play();
	}
	
	/// <summary>
	/// Plays the animation in reverse
	/// </summary>
	public void Reverse()
	{
		Reverse(null);
	}
	/// <summary>
	/// Plays the animation in reverse
	/// </summary>
	/// <param name="callback">The callback function to be called at the end of the animation</param>
	public void Reverse(Action callback)
	{
		if (!Target.activeSelf) return;
		
		this.callback = callback;
		
		_isPaused = false;
		
		if (!IsPlaying)
		{
			_isPlaying = _isReversing = true;
			onStart.Invoke(this);
			
			if (!_hasReverted)
			{
				ReverseDirection();
			}
			
			StartTween();
		}
	}

	/// <summary>
	/// Plays the animation in reverse with the specified tag.
	/// </summary>
	/// <param name="tag">Tag of the animation.</param>
	public void ReverseWithTag(string tag)
	{
		if (Tag == tag)
			Reverse();
		else
			GetTweenWithTag(tag).Reverse();
	}

	public void Pause()
	{
		_isPaused = true;
	}

	public void PauseWithTag(string tag)
	{
		if (Tag == tag)
			Pause();
		else
			GetTweenWithTag(tag).Pause();
	}
	
	public void Stop()
	{
		_isPlaying = false;
		_isReversing = false;
		
		ElapsedDelay = 0;
		ElapsedTime = 0;
	}

	public void StopWithTag(string tag)
	{
		if (Tag == tag)
			Stop();
		else
			GetTweenWithTag(tag).Stop();
	}
	
	public void Reset()
	{
		Stop();
		UpdateAll(0);
	}
	
	public void Clear()
	{
		Initialize();
	}
	
	public void InitialState()
	{
		UpdateAll(0);
	}
	
	public void FinalState()
	{
		UpdateAll(1);
	}

	private PotaTween GetTweenWithTag(string tag)
	{
		foreach (PotaTween tween in GetComponents<PotaTween>()) 
		{
			if (tween.Tag == tag)
			{
				return tween;
			}
		}
		
		Debug.LogError("Tween with tag \"" + tag + "\" not found.");
		return null;
	}
	
	#endregion
	
	#region Initialize
	void Initialize()
	{		
		Position = new PTTVector3(OwnTransform.position);
		Rotation = new PTTVector3(OwnTransform.eulerAngles);
		Scale = new PTTVector3(OwnTransform.localScale);
		if (GetComponent<Renderer>() != null && GetComponent<Renderer>().sharedMaterial != null)
		{
			Color =
				new PTTColor((GetComponent<Renderer>().sharedMaterial.HasProperty(_colorProperty)
				              ? GetComponent<Renderer>().sharedMaterial.GetColor(_colorProperty)
				              : UnityEngine.Color.white));
			Alpha =
				new PTTFloat((GetComponent<Renderer>().sharedMaterial.HasProperty(_colorProperty)
				              ? GetComponent<Renderer>().sharedMaterial.GetColor(_colorProperty).a
				              : 1));
		}
		else
		{
			Color = new PTTColor(UnityEngine.Color.white);
			Alpha = new PTTFloat(1);
		}
		Float = new PTTFloat(0);
		
		Duration = 1;
		Speed = 0;
		Delay = 0;
		SetEaseEquation(Ease.Equation.Linear);
		_startCallback = null;
		_updateCallback = null;
		finishCallback.Clear();
		_hasReverted = false;
		
		_loopcount = 0;
		
		Curve = AnimationCurve.Linear(0, 0, 1, 1);
		Loop = LoopType.None;
	}
	#endregion
	
	#region Tween
	void StartTween()
	{
		StartFrom(0);
	}
	
	void StartFrom(float time)
	{
		ElapsedTime = time;
		ElapsedDelay = time;
		
		CalculateDuration();
		
		GetRenderers();
		InstantiateMaterials();
		
		UpdateAll(CalculateEase());
		
		_isPlaying = true;
		_hasCompleted = false;
		
		if (_startCallback != null)
			_startCallback();
	}
	
	private bool DelayLoop()
	{
		if (ElapsedDelay < Delay)
		{
			ElapsedDelay += Time.deltaTime;
			return true;
		}
		
		return false;
	}
	
	private bool TweenLoop()
	{
		if (ElapsedTime < Duration)
		{
			ElapsedTime += Time.deltaTime;
			UpdateAll(CalculateEase());


			if (_updateCallback != null)
				_updateCallback();
			CheckTimedCallback();
			
			return true;
		}
		
		return false;
	}
	
	private void TweenCompleted()
	{
		if (!HasCompleted && IsPlaying)
		{
			_hasCompleted = true;
			
			ElapsedTime = Duration;
			UpdateAll(1);
			
			_isPlaying = false;		
			onComplete.Invoke(this);
			
			if (callback != null)
			{
				callback();
				callback = null;
			}

		    for (int i = 0; i < this.finishCallback.Count; i++)
		    {
		        finishCallback[i](gameObject);
		    }
			
			ReturnMaterials();
			
			if (Loop != LoopType.None)
			{
				_loopcount++;
				
				if (LoopsNumber == 0 || _loopcount < LoopsNumber)
				{
					if (Loop == LoopType.PingPong)
						ReverseDirection();
					
					StartTween();
				}
			}
		}
	}
	#endregion
	
	#region CalculateDuration
	float positionDuration, rotationDuration, scaleDuration, colorDuration, alphaDuration;
	List<float> durationsList = new List<float>();
	void CalculateDuration()
	{
		if (Speed <= 0) return;
		
		durationsList = new List<float>();
		
		if (Position.From != Position.To)
		{
			positionDuration = Mathf.Abs(Vector3.Distance(Position.From, Position.To) / Speed);
			durationsList.Add(positionDuration);
		}
		if (Scale.From != Scale.To)
		{
			scaleDuration = Mathf.Abs(Vector3.Distance(Scale.From, Scale.To) / Speed);
			durationsList.Add(scaleDuration);
		}
		if (Rotation.From != Rotation.To)
		{
			rotationDuration = Mathf.Abs((Vector3.Angle(Rotation.From, Rotation.To)) / Speed);
			durationsList.Add(rotationDuration);
		}
		if (Color.From != Color.To)
		{
			colorDuration = Mathf.Abs(Vector4.Distance((Vector4)Color.From, (Vector4)Color.To) / Speed);
			durationsList.Add(colorDuration);
		}
		if (Alpha.From != Alpha.To)
		{
			alphaDuration = Mathf.Abs((Alpha.From - Alpha.To) / Speed);
			durationsList.Add(alphaDuration);
		}
		
		if (durationsList.Count > 0)
			Duration = durationsList.Max();
	}
	#endregion
	
	#region Updates
	void UpdateAll(float value)
	{
		_easeValue = value;
		
		if (Target == null) return;
		
		UpdatePosition();
		UpdateRotation();
		UpdateScale();
		UpdateColor();
		UpdateAlpha();
		UpdateFloat();
	}
	
	float CalculateEase()
	{
		if (ElapsedTime > Duration)
			ElapsedTime = Duration;
		
		return (EasingReference == EasingReference.Equation) ? (float)easeMethod.Invoke(null, new object[] { ElapsedTime, 0, 1, Duration }) : 
			Curve.Evaluate(ElapsedTime / Duration);
	}

	float CalculateLerp(float from, float to)
	{
		return from + _easeValue * (to - from);
	}
	
	#region Position
	void UpdatePosition()
	{
		if (Position.From == Position.To || ((OwnTransform.position == Position.To || OwnTransform.localPosition == Position.To) && 
		                                     ElapsedTime >= Duration)) return;


		Vector3 p = Position.IsLocal ? OwnTransform.localPosition : OwnTransform.position;
		Vector3 add = Position.IsRelative ? _startPosition : Vector3.zero;


        p.x = Position.IgnoreAxis.X ? p.x : CalculateLerp(Position.From.x, Position.To.x) + add.x;
		p.y = Position.IgnoreAxis.Y ? p.y : CalculateLerp(Position.From.y, Position.To.y) + add.y;
		p.z = Position.IgnoreAxis.Z ? p.z : CalculateLerp(Position.From.z, Position.To.z) + add.z;

        if (Position.IsLocal)
        {
            OwnTransform.localPosition = p;
		}
		else
		{
			if (_rigidbody2D != null)
				_rigidbody2D.MovePosition(p);
			else if (_rigidbody != null)
				_rigidbody.MovePosition(p);
			else
				OwnTransform.position = p;
		}
    }
	#endregion
	
	#region Rotation
	void UpdateRotation()
	{
		if (Rotation.From == Rotation.To || (OwnTransform.eulerAngles == Rotation.To && ElapsedTime >= Duration)) return;
		
		Vector3 r = Rotation.IsLocal ? OwnTransform.localEulerAngles : OwnTransform.eulerAngles;
		Vector3 add = Rotation.IsRelative ? _startRotation : Vector3.zero;

		r.x = Rotation.IgnoreAxis.X ? r.x : CalculateLerp(Rotation.From.x, Rotation.To.x) + add.x;
		r.y = Rotation.IgnoreAxis.Y ? r.y : CalculateLerp(Rotation.From.y, Rotation.To.y) + add.y;
		r.z = Rotation.IgnoreAxis.Z ? r.z : CalculateLerp(Rotation.From.z, Rotation.To.z) + add.z;
		
		if (Rotation.IsLocal)
			OwnTransform.localRotation = Quaternion.Euler(r);
		else
			OwnTransform.rotation = Quaternion.Euler(r);
		
	}
	#endregion
	
	#region Scale
	void UpdateScale()
	{
		if (Scale.From == Scale.To || (OwnTransform.localScale == Scale.To && ElapsedTime >= Duration)) return;
		
		Vector3 s = OwnTransform.localScale;
		Vector3 add = Scale.IsRelative ? _startScale : Vector3.zero;

		s.x = Scale.IgnoreAxis.X ? s.x : CalculateLerp(Scale.From.x, Scale.To.x) + add.x;
		s.y = Scale.IgnoreAxis.Y ? s.y : CalculateLerp(Scale.From.y, Scale.To.y) + add.y;
		s.z = Scale.IgnoreAxis.Z ? s.z : CalculateLerp(Scale.From.z, Scale.To.z) + add.z;

		OwnTransform.localScale = s;
	}
	#endregion
	
	#region Color
	void UpdateColor()
	{
		if (Color.From == Color.To) return;

		Color newColor = Color.From + _easeValue * (Color.To - Color.From);

		for (int i = 0; i < _renderers.Length; i++)
		{
			if (_renderers[i] == null) continue;
			
			TextMesh tm = _renderers[i].GetComponent<TextMesh>();

			Text t = _renderers[i].GetComponent<Text>();
			
			if (tm)
			{
				tm.color = newColor;
			} else if (t) {
				t.color = newColor;
			}
			else if (_renderers[i] is SpriteRenderer)
			{
				SpriteRenderer render = (SpriteRenderer)_renderers[i];
				render.color = newColor;
			}
			else if (_renderers[i].sharedMaterial.HasProperty(_colorProperty))
			{
				_renderers[i].sharedMaterial.SetColor(_colorProperty, newColor);
			}
		}

		if (_texts != null) {
			for (int j = 0; j < _texts.Length; j++)
			{
				_texts[j].color = newColor;
			}
		}
		if (_textsPro != null) {
			for (int j = 0; j < _textsPro.Length; j++)
			{
				_textsPro[j].color = newColor;
			}
		}
		if (_textsProUGUI != null) {
			for (int j = 0; j < _textsProUGUI.Length; j++)
			{
				_textsProUGUI[j].color = newColor;
			}
		}
		
		if (_images == null)
			return;
		
		for (int j = 0; j < _images.Length; j++)
		{
			_images[j].color = newColor;
		}
	}
	
	void InstantiateMaterials()
	{
		if (Color.From == Color.To && Alpha.From == Alpha.To) return;
		
		List<Material> refMaterials = new List<Material>();
		for (int i = 0; i < _renderers.Length; i++)
		{
			if (!refMaterials.Contains(_renderers[i].sharedMaterial))
			{
				refMaterials.Add(_renderers[i].sharedMaterial);
			}
		}
		
		List<Material> cloneMaterials = new List<Material>();
		for (int i = 0; i < refMaterials.Count; i++)
		{
			cloneMaterials.Add(refMaterials[i] == null ? null : new Material(refMaterials[i]));
		}
		
		for (int i = 0; i < _renderers.Length; i++)
		{
			for (int j = 0; j < refMaterials.Count; j++)
			{
				if (_renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial == refMaterials[j])
				{
					_renderers[i].sharedMaterial = cloneMaterials[j];
				}
			}
		}
	}
	
	void ReturnMaterials()
	{
		for (int i = 0; i < _renderers.Length; i++)
		{
			if (_renderers[i] != null && _bkpMaterials[i] != null && _renderers[i].sharedMaterial != null && _renderers[i].sharedMaterial.HasProperty(_colorProperty))
			{
				if (_renderers[i].sharedMaterial.GetColor(_colorProperty) == _bkpMaterials[i].GetColor(_colorProperty))
				{
					_renderers[i].sharedMaterial = _bkpMaterials[i];
				}
			}
		}
	}
	#endregion
	
	#region Alpha
	void UpdateAlpha()
	{
		if (Alpha.From == Alpha.To) return;

		Color newColor = new Color();
		float alpha = Mathf.Clamp01(CalculateLerp(Alpha.From, Alpha.To));

		for (int i = 0; i < _renderers.Length; i++)
		{
			if (_renderers[i] == null) continue;
			
			TextMesh tm = _renderers[i].GetComponent<TextMesh>();

			if (tm)
			{
				newColor = tm.color;
				newColor.a = alpha;
				tm.color = newColor;
			}
			else if (_renderers[i] is SpriteRenderer)
			{
				SpriteRenderer render = (SpriteRenderer)_renderers[i];
				newColor = render.color;
				newColor.a = alpha;
				render.color = newColor;
			}
			
			//TODO: rever uso de shared materials
			else if (_renderers[i].material == null)
				continue;
			else if (_renderers[i].material.HasProperty(_colorProperty))
			{
				newColor = _renderers[i].material.GetColor(_colorProperty);
				newColor.a = alpha;
				_renderers[i].material.SetColor(_colorProperty, newColor);
			}
		}
		
		if (_texts != null) {
			for (int j = 0; j < _texts.Length; j++)
			{
				newColor = _texts[j].color;
				newColor.a = alpha;
				if (_texts[j]) _texts[j].color = newColor;
			}
		}
		if (_textsPro != null) {
			for (int j = 0; j < _textsPro.Length; j++)
			{
				newColor = _textsPro[j].color;
				newColor.a = alpha;
				if (_textsPro[j]) _textsPro[j].color = newColor;
			}
		}
		if (_textsProUGUI != null) {
			for (int j = 0; j < _textsProUGUI.Length; j++)
			{
				newColor = _textsProUGUI[j].color;
				newColor.a = alpha;
				if (_textsProUGUI[j]) _textsProUGUI[j].color = newColor;
			}
		}

		if (_images == null)
			return;
		
		for (int j = 0; j < _images.Length; j++)
		{
			newColor = _images[j].color;
			newColor.a = alpha;
			if (_images[j]) _images[j].color = newColor;
		}
	}
	#endregion
	
	#region Float
	void UpdateFloat()
	{
		if (Float.From == Float.To || (Float.Value == Float.To && ElapsedTime >= Duration)) return;
		
		Float.Value = CalculateLerp(Float.From, Float.To);
	}
	#endregion
	
	#region TimedCallbacks
	void CheckTimedCallback()
	{
		if (timedCallbacks == null) return;
		
		for (int i = 0; i < timedCallbacks.Count; i++)
		{
			if (timedCallbacks[i].Time >= ElapsedTime - Time.deltaTime && timedCallbacks[i].Time <= ElapsedTime)
			{
				timedCallbacks[i].Action();
			}
		}
	}
	#endregion
	#endregion
	
	#region Revert
	void ReverseDirection()
	{
		_hasReverted = !_hasReverted;
		
		Position = new PTTVector3(Position.To, Position.From, Position.IsLocal, Position.IsRelative, Position.IgnoreAxis);
		Rotation = new PTTVector3(Rotation.To, Rotation.From, Rotation.IsLocal, Rotation.IsRelative, Rotation.IgnoreAxis);
		Scale = new PTTVector3(Scale.To, Scale.From, Scale.IsLocal, Scale.IsRelative, Scale.IgnoreAxis);
		Color = new PTTColor(Color.To, Color.From);
		Alpha = new PTTFloat(Alpha.To, Alpha.From);
		Float = new PTTFloat(Float.To, Float.From);
		
		if (Curve != null && FlipCurveOnReverse)
			Curve = ReverseCurve(Curve);
	}
	
	AnimationCurve ReverseCurve(AnimationCurve curve)
	{
		Keyframe[] keys = new Keyframe[curve.keys.Length];
		float totalTime = curve.keys[keys.Length - 1].time;
		
		for (int i = keys.Length - 1; i >= 0; i--)
		{
			Keyframe key = curve.keys[keys.Length - 1 - i];
			
			keys[i] = new Keyframe(totalTime - key.time, 1 - key.value, key.inTangent, key.outTangent);
		}
		
		return new AnimationCurve(keys);
	}
	
	#endregion
}

#region EasingEquations
public class Ease
{
	public enum Equation
	{
		Linear,
		OutExpo, InExpo, InOutExpo, OutInExpo,
		OutCirc, InCirc, InOutCirc, OutInCirc,
		//OutQuad, InQuad, InOutQuad, OutInQuad,
		OutSine, InSine, InOutSine, OutInSine,
		//OutCubic, InCubic, InOutCubic, OutInCubic,
		//OutQuartic, InQuartic, InOutQuartic, OutInQuartic, 
		//OutQuintic, InQuintic, InOutQuintic, OutInQuintic,
		OutElastic, InElastic, InOutElastic, OutInElastic,
		OutBounce, InBounce, InOutBounce, OutInBounce,
		OutBack, InBack, InOutBack, OutInBack
	}
	
	#region Linear
	public static float Linear(float time, float start, float end, float duration)
	{
		return end * time / duration + start;
	}
	#endregion
	
	#region Expo
	public static float OutExpo(float time, float start, float end, float duration)
	{
		return (time == duration) ? start + end : end * (-Mathf.Pow(2, -10 * time / duration) + 1) + start;
	}
	
	public static float InExpo(float time, float start, float end, float duration)
	{
		return (time == 0) ? start : end * Mathf.Pow(2, 10 * (time / duration - 1)) + start;
	}
	
	public static float InOutExpo(float time, float start, float end, float duration)
	{
		if (time == 0)
			return start;
		
		if (time == duration)
			return start + end;
		
		if ((time /= duration / 2) < 1)
			return end / 2 * Mathf.Pow(2, 10 * (time - 1)) + start;
		
		return end / 2 * (-Mathf.Pow(2, -10 * --time) + 2) + start;
	}
	
	public static float OutInExpo(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return OutExpo(time * 2, start, end / 2, duration);
		
		return InExpo((time * 2) - duration, start + end / 2, end / 2, duration);
	}
	#endregion
	
	#region Circular
	public static float OutCirc(float time, float start, float end, float duration)
	{
		return end * Mathf.Sqrt(1 - (time = time / duration - 1) * time) + start;
	}
	
	public static float InCirc(float time, float start, float end, float duration)
	{
		return -end * (Mathf.Sqrt(1 - (time /= duration) * time) - 1) + start;
	}
	
	public static float InOutCirc(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return -end / 2 * (Mathf.Sqrt(1 - time * time) - 1) + start;
		
		return end / 2 * (Mathf.Sqrt(1 - (time -= 2) * time) + 1) + start;
	}
	
	public static float OutInCirc(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return OutCirc(time * 2, start, end / 2, duration);
		
		return InCirc((time * 2) - duration, start + end / 2, end / 2, duration);
	}
	#endregion
	
	#region Quad
	#endregion
	
	#region Sine
	public static float OutSine(float time, float start, float end, float duration)
	{
		return end * Mathf.Sin(time / duration * (Mathf.PI / 2)) + start;
	}
	
	public static float InSine(float time, float start, float end, float duration)
	{
		return -end * Mathf.Cos(time / duration * (Mathf.PI / 2)) + end + start;
	}
	
	public static float InOutSine(float time, float start, float end, float duration)
	{
		if ((time /= duration / 2) < 1)
			return end / 2 * (Mathf.Sin(Mathf.PI * time / 2)) + start;
		
		return -end / 2 * (Mathf.Cos(Mathf.PI * --time / 2) - 2) + start;
	}
	
	public static float OutInSine(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return OutSine(time * 2, start, end / 2, duration);
		
		return InSine((time * 2) - duration, start + end / 2, end / 2, duration);
	}
	#endregion
	
	#region Cubic
	#endregion
	
	#region Quartic
	#endregion
	
	#region Quintic
	#endregion
	
	#region Elastic
	public static float OutElastic(float time, float start, float end, float duration)
	{
		if ((time /= duration) == 1)
			return start + end;
		
		float p = duration * 0.3f;
		float s = p / 4;
		
		return (end * Mathf.Pow(2, -10 * time) * Mathf.Sin((time * duration - s) * (2 * Mathf.PI) / p) + end + start);
	}
	
	public static float InElastic(float time, float start, float end, float duration)
	{
		if ((time /= duration) == 1)
			return start + end;
		
		float p = duration * 0.3f;
		float s = p / 4;
		
		return -(end * Mathf.Pow(2, 10 * (time -= 1)) * Mathf.Sin((time * duration - s) * (2 * Mathf.PI) / p)) + start;
	}
	
	public static float InOutElastic(float time, float start, float end, float duration)
	{
		if ((time /= duration / 2) == 2)
			return start + end;
		
		float p = duration * (0.3f * 1.5f);
		float s = p / 4;
		
		if (time < 1)
			return -0.5f * (end * Mathf.Pow(2, 10 * (time -= 1)) * Mathf.Sin((time * duration - s) * (2 * Mathf.PI) / p)) + start;
		
		return end * Mathf.Pow(2, -10 * (time -= 1)) * Mathf.Sin((time * duration - s) * (2 * Mathf.PI) / p) * 0.5f + end + start;
	}
	
	public static float OutInElastic(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return OutElastic(time * 2, start, end / 2, duration);
		
		return InElastic((time * 2) - duration, start + end / 2, end / 2, duration);
	}
	#endregion
	
	#region Bounce
	public static float OutBounce(float time, float start, float end, float duration)
	{
		if ((time /= duration) < (1 / 2.75f))
			return end * (7.5625f * time * time) + start;
		else if (time < (2 / 2.75f))
			return end * (7.5625f * (time -= (1.5f / 2.75f)) * time + 0.75f) + start;
		else if (time < (2.5f / 2.75f))
			return end * (7.5625f * (time -= (2.25f / 2.75f)) * time + 0.9375f) + start;
		else
			return end * (7.5625f * (time -= (2.625f / 2.75f)) * time + 0.984375f) + start;
	}
	
	public static float InBounce(float time, float start, float end, float duration)
	{
		return end - OutBounce(duration - time, 0, end, duration) + start;
	}
	
	public static float InOutBounce(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return InBounce(time * 2, 0, end, duration) * 0.5f + start;
		
		return OutBounce(time * 2 - duration, 0, end, duration) * 0.5f + end * 0.5f + start;
	}
	
	public static float OutInBounce(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return OutBounce(time * 2, start, end / 2, duration);
		
		return InBounce((time * 2) - duration, start + end / 2, end / 2, duration);
	}
	#endregion
	
	#region Back
	public static float OutBack(float time, float start, float end, float duration)
	{
		return end * ((time = time / duration - 1) * time * ((1.70158f + 1) * time + 1.70158f) + 1) + start;
	}
	
	public static float InBack(float time, float start, float end, float duration)
	{
		return end * (time /= duration) * time * ((1.70158f + 1) * time - 1.70158f) + start;
	}
	
	public static float InOutBack(float time, float start, float end, float duration)
	{
		float s = 1.70158f;
		if ((time /= duration / 2) < 1)
			return end / 2 * (time * time * (((s *= (1.525f)) + 1) * time - s)) + start;
		
		return end / 2 * ((time -= 2) * time * (((s *= (1.525f)) + 1) * time + s) + 2) + start;
	}
	
	public static float OutInBack(float time, float start, float end, float duration)
	{
		if (time < duration / 2)
			return OutBack(time * 2, start, end / 2, duration);
		
		return InBack((time * 2) - duration, start + end / 2, end / 2, duration);
	}
	#endregion
}
#endregion
