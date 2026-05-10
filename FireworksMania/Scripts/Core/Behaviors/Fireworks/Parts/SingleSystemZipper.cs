//Credit goes to Panini for creating the original version of this script and allowing me to use it as a base for this one.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    [ExecuteAlways]
    public class SingleSystemZipper : MonoBehaviour
    {
        [ReadOnly]
        [Header("=========== Zipper Player Settings ===========")]
        [Space(30)]
        public string EffectPlayer = "SINGLE SYSTEM ZIPPER PLAYER";

        [ReadOnly]
        [Foldout("Zipper Info")]
        [Header("Read Only - Total Number of Fan Bursts")]
        [Tooltip("The total numbe of time an effect will be played in the zipper")]
        public string TotalEffects = "0 Effects";

        [ReadOnly]
        [Foldout("Zipper Info")]
        [Header("Read Only - Single Fan Burst Time")]
        [Tooltip("The estimated total time of the zipper effect for 1 play through")]
        public string TimePerCycle = "0 Seconds";

        [ReadOnly]
        [Foldout("Zipper Info")]
        [Header("Read Only - Total Duration of Fan")]
        [Tooltip("The estimated total duration of the zipper")]
        public string TotalDuration = "Play Zipper Once to Set Total Time";

        [ReadOnly]
        [Foldout("Zipper Info")]
        [Header("Read Only - Current Time")]
        [Tooltip("The estimated time of the effect when playing in seconds")]
        public string LocalTime = "0 Seconds";

        private float CurrTime;

        [Foldout("Zipper Settings")]
        [Space(10)]
        [Header("Fan Type")]
        [Tooltip("This is the Type of Fan you want to create")]
        public DirectionalType FanType = DirectionalType.LeftToRight;

        [Foldout("Zipper Settings")]
        [Header("Particle System")]
        [Tooltip("This is the Particle System that the Fan will be created under\n\nThe Position and Rotation of this particle system will effect the position and Rotation of the Fan")]
        public ParticleSystem MainSystem;

        [MinValue(0), MaxValue(1000)]
        [Foldout("Zipper Settings")]
        [Header("Number of Bursts")]
        [Tooltip("This is the number of launch effects you want in your zipper")]
        public int NumberOfBursts = 3;

        [MinValue(0f), MaxValue(45f)]
        [Foldout("Zipper Settings")]
        [Header("Angle Between Effects")]
        [Tooltip("This is the angle between each launch effect in the fan")]
        public float AngleBetweenBurst = 10f;

        [ShowIf("IsUniform")]
        [MinValue(0f), MaxValue(10f)]
        [Foldout("Zipper Settings")]
        [Header(" X Position Range For Effects")]
        [Tooltip("This is the X position difference between each launch effect in the fan\n this can be used to create a more realistic cake launching effect")]
        public float XDistance = 1f;
        private float BurstPos;

        [ShowIf("IsUniform")]
        [MinValue(0f), MaxValue(10f)]
        [Foldout("Zipper Settings")]
        [Header(" Z Position Range For Cycles")]
        [Tooltip("This is the Z position difference between each launch effect in the fan\n this can be used to create a more realistic cake launching effect")]
        public float ZDistance = 1f;
        private float CyclePos;

        [MinValue(0.001f), MaxValue(1000f)]
        [Foldout("Zipper Settings")]
        [Header("Time Delay Between Launch Effects")]
        [Tooltip("This is the time between each launch effect in the Fan")]
        public float TimeBetweenBursts = 1f;

        [MinValue(0), MaxValue(1000)]
        [Foldout("Zipper Settings")]
        [Header("Number of Cycles")]
        [Tooltip("This is the number of times your fan effect will play\nIf set to one the zipper will fire all its launch effects once\nIf set to two the zipper will fire all its launch effects and then play them again in the reverse direction\n\nUniform Zippers will fire back and forth in one cycle then repeat for the next cycle")]
        public int NumberOfCycles = 1;

        [MinValue(0.001f), MaxValue(1000f)]
        [Foldout("Zipper Settings")]
        [Header("Time Delay Between Cycles")]
        [Tooltip("This is the time between each Cycle in the Fan Effect")]
        public float TimeBetweenCycles = 0f;

        [MinValue(0f), MaxValue(1000f)]
        [Foldout("Zipper Settings")]
        [Header("Start Delay")]
        [Tooltip("Delay the start of the particle system by this number of seconds")]
        public float StartDelay = 0f;


        [HideInInspector]
        public bool IsUniform = false;
        private bool Started = false;

        [Foldout("Advanced Settings")]
        [Header("Update Paticle System On Value Change")]
        [Tooltip("This allows you disable the auto updating of the main particle system settings")]
        public bool UpdateParticleSystemAuto = true;

        [Foldout("Advanced Settings")]
        [Header("Delays between each cycle")]
        [Tooltip("This allows you to set a specific time in between each individual cycle")]
        public bool EnableCustomDelays = false;

        [ShowIf("EnableCustomDelays")]
        [Foldout("Advanced Settings")]
        [Tooltip("This is the value of time between each cycle")]
        [SerializeField]
        public List<float> CycleDelays = new List<float>();

        [Foldout("Advanced Settings")]
        [Header("Change Colors between each cycle")]
        [Tooltip("This allows you to set a specific Color for each individual cycle")]
        public bool EnableCustomColors = false;

        [ShowIf("EnableCustomColors")]
        [Foldout("Advanced Settings")]
        [Tooltip("This is the StartColor value between each cycle")]
        [SerializeField]
        public List<Color> CycleColors = new List<Color>();

        public enum DirectionalType
        {
            LeftToRight = 0,
            RightToLeft = 1,
            LeftZipper = 2,
            RightZipper = 3,
            Pyramid = 4,
            PyramidZipper = 5,
        }

        private void OnValidate()
        {


            if (!EnableCustomDelays)
            {
                SetCycles();
            }

            if (FanType == DirectionalType.Pyramid || FanType == DirectionalType.PyramidZipper)
            {
                IsUniform = false;
            }
            else
            {
                IsUniform = true;
            }

            BurstPos = (XDistance / NumberOfBursts);
            CyclePos = (ZDistance / NumberOfCycles);

#if UNITY_EDITOR
            TotalDuration = GetTime().ToString() + " seconds";
            int effectnumber = NumberOfBursts * NumberOfCycles;
            TotalEffects = effectnumber.ToString() + " effects";
            float cycleduration = NumberOfBursts * TimeBetweenBursts;
            TimePerCycle = cycleduration.ToString() + " seconds";

            if (this.MainSystem == null)
            {
                ParticleSystem main;
                if (this.gameObject.TryGetComponent<ParticleSystem>(out main))
                {
                    this.MainSystem = main;
                }
            }
            else
            {
                if (this.MainSystem.isStopped && UpdateParticleSystemAuto)
                {
                    UpdateParticleSystem();
                }
            }
#endif
        }

        public void UpdateParticleSystem()
        {
            var main = MainSystem.main;

            float time = GetTime();
            main.duration = time + 5;

            if (CycleDelays.Count >= 1)
            {
                main.startDelay = StartDelay;
            }


            var emission = MainSystem.emission;

            emission.burstCount = 0;
            emission.rateOverTime = 0;
            emission.rateOverDistance = 0;
        }

        private float GetTime()
        {
            float Time = (((TimeBetweenBursts * NumberOfBursts) + TimeBetweenBursts) * NumberOfCycles) + StartDelay;
            for (int i = 0; i <= CycleDelays.Count - 1; i++)
            {
                Time += CycleDelays[i];
            }
            return Time;
        }

        // Start is called before the first frame update
        void Start()
        {
            Started = false;
            if (Application.isPlaying)
            {
                if (MainSystem != null)
                {
                    if (Started == false)
                    {
                        BurstPos = (XDistance / NumberOfBursts);
                        CyclePos = (ZDistance / NumberOfCycles);
#if UNITY_EDITOR
                        Debug.Log("Starting Animation");
#endif
                        Started = true;
                        StartAnimation();
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (Started == false)
                {
                    if (MainSystem.isPlaying)
                    {
                        Debug.Log("Starting Animation");
                        Started = true;
                        StartAnimation();
                    }
                }
                if (Started)
                {
                    if (MainSystem.isStopped)
                    {
                        StopAllCoroutines();
                        Started = false;
                        CurrTime = 0;
                        ZeroOut();
                    }
                }
            }

            LocalTime = CurrTime.ToString("000.00") + " Seconds";

        }
#endif

        public void ZeroOut()
        {
            MainSystem.gameObject.transform.localPosition = new Vector3(0, MainSystem.gameObject.transform.localPosition.y, 0);
            MainSystem.gameObject.transform.localRotation = Quaternion.identity;
        }


        public void StartAnimation()
        {
            CurrTime = 0;
            if (FanType == DirectionalType.Pyramid || FanType == DirectionalType.PyramidZipper)
            {

                StartCoroutine(PyramidAnimator());
#if UNITY_EDITOR
                Debug.Log("Pyramid Animation");
#endif
            }
            else
            {
                StartCoroutine(UniformAnmimator());
#if UNITY_EDITOR
                Debug.Log("Uniform Animation");
#endif
            }
        }

        private IEnumerator PyramidAnimator()
        {

            yield return new WaitForSeconds(StartDelay);
            CurrTime += StartDelay;

            if (FanType == DirectionalType.Pyramid)
            {
                float CycleModifier = (ZDistance / 2);
                for (int i = 0; i <= NumberOfCycles - 1; i++)
                {
                    float StartAngle = StartanlgePyramid();
                    float StartPos = StartPosPyramid();
#if UNITY_EDITOR
                    Debug.Log($"StartAngle = {StartAngle}");

                    Debug.Log("Cycle: " + i);
#endif

                    Vector3 Cyclepos = new Vector3(MainSystem.gameObject.transform.localPosition.x, MainSystem.transform.localPosition.y, CycleModifier);

                    MainSystem.transform.localPosition = Cyclepos;
                    CycleModifier -= CyclePos;
                    if (i != 0)
                    {
                        yield return new WaitForSeconds(CycleDelays[i - 1]);
                        CurrTime += CycleDelays[i - 1];
                    }

                    for (int j = 0; j <= ((NumberOfBursts - 1) / 2); j++)
                    {

                        if (j != 0)
                        {
                            yield return new WaitForSeconds(TimeBetweenBursts);
                            CurrTime += TimeBetweenBursts;
                        }


                        var setangle2 = new Vector3(0, 0, StartAngle);
                        var setangle3 = new Vector3(0, 0, -StartAngle);

                        MainSystem.gameObject.transform.localEulerAngles = setangle2;
                        var emitParams2 = new ParticleSystem.EmitParams();
                        if (EnableCustomColors)
                        {
                            emitParams2.startColor = CycleColors[i];

                        }
                        MainSystem.Emit(emitParams2, 1);
#if UNITY_EDITOR
                        Debug.Log("Setting rotation 1 to: " + setangle2 + " Time: " + CurrTime.ToString("00.00"));
#endif

                        if (StartAngle != 0)
                        {
                            MainSystem.gameObject.transform.localEulerAngles = setangle3;
                            var emitParams3 = new ParticleSystem.EmitParams();
                            if (EnableCustomColors)
                            {
                                emitParams3.startColor = CycleColors[i];

                            }
                            MainSystem.Emit(emitParams3, 1);
#if UNITY_EDITOR
                            Debug.Log("Setting rotation 2 to: " + setangle3 + " Time: " + CurrTime.ToString("00.00"));
#endif
                        }

                        StartAngle += AngleBetweenBurst;
                        StartPos += BurstPos;
                    }
                }
            }

            if (FanType == DirectionalType.PyramidZipper)
            {
                float StartAngle = StartanlgePyramid();
                float StartPos = StartPosPyramid();
                float CycleModifier = (ZDistance / 2);
                for (int i = 0; i <= NumberOfCycles - 1; i++)
                {
#if UNITY_EDITOR
                    Debug.Log($"StartAngle = {StartAngle}");

                    Debug.Log("Cycle: " + i);
#endif

                    Vector3 Cyclepos = new Vector3(MainSystem.gameObject.transform.localPosition.x, MainSystem.transform.localPosition.y, CycleModifier);

                    MainSystem.transform.localPosition = Cyclepos;
                    CycleModifier -= CyclePos;
                    if (i != 0)
                    {
                        yield return new WaitForSeconds(CycleDelays[i - 1]);
                        CurrTime += CycleDelays[i - 1];
                    }

                    if (i % 2 == 0)
                    {
                        for (int j = 0; j <= ((NumberOfBursts - 1) / 2); j++)
                        {

                            if (j != 0)
                            {
                                yield return new WaitForSeconds(TimeBetweenBursts);
                                CurrTime += TimeBetweenBursts;
                            }


                            var setangle2 = new Vector3(0, 0, StartAngle);
                            var setangle3 = new Vector3(0, 0, -StartAngle);

                            MainSystem.gameObject.transform.localEulerAngles = setangle2;
                            var emitParams2 = new ParticleSystem.EmitParams();
                            if (EnableCustomColors)
                            {
                                emitParams2.startColor = CycleColors[i];

                            }
                            MainSystem.Emit(emitParams2, 1);
#if UNITY_EDITOR
                            Debug.Log("Setting rotation 1 to: " + setangle2 + " Time: " + CurrTime.ToString("00.00"));
#endif
                            if (StartAngle != 0)
                            {
                                MainSystem.gameObject.transform.localEulerAngles = setangle3;
                                var emitParams3 = new ParticleSystem.EmitParams();
                                if (EnableCustomColors)
                                {
                                    emitParams3.startColor = CycleColors[i];

                                }
                                MainSystem.Emit(emitParams3, 1);
#if UNITY_EDITOR
                                Debug.Log("Setting rotation 2 to: " + setangle3 + " Time: " + CurrTime.ToString("00.00"));
#endif
                            }

                            if (j < ((NumberOfBursts - 1) / 2))
                            {
                                StartAngle += AngleBetweenBurst;
                                StartPos += BurstPos;
                            }
                            else
                            {
                                yield return new WaitForSeconds(TimeBetweenBursts);
                                CurrTime += TimeBetweenBursts;
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j <= ((NumberOfBursts - 1) / 2); j++)
                        {

                            if (j != 0)
                            {
                                yield return new WaitForSeconds(TimeBetweenBursts);
                                CurrTime += TimeBetweenBursts;
                            }


                            var setangle2 = new Vector3(0, 0, StartAngle);
                            var setangle3 = new Vector3(0, 0, -StartAngle);

                            MainSystem.gameObject.transform.localEulerAngles = setangle2;
                            var emitParams2 = new ParticleSystem.EmitParams();
                            if (EnableCustomColors)
                            {
                                emitParams2.startColor = CycleColors[i];

                            }
                            MainSystem.Emit(emitParams2, 1);
#if UNITY_EDITOR
                            Debug.Log("Setting rotation 1 to: " + setangle2 + " Time: " + CurrTime.ToString("00.00"));
#endif
                            if (StartAngle != 0)
                            {
                                MainSystem.gameObject.transform.localEulerAngles = setangle3;
                                var emitParams3 = new ParticleSystem.EmitParams();
                                if (EnableCustomColors)
                                {
                                    emitParams3.startColor = CycleColors[i];

                                }
                                MainSystem.Emit(emitParams3, 1);
#if UNITY_EDITOR
                                Debug.Log("Setting rotation 2 to: " + setangle3 + " Time: " + CurrTime.ToString("00.00"));
#endif
                            }

                            if (j < ((NumberOfBursts - 1) / 2))
                            {
                                StartAngle -= AngleBetweenBurst;
                                StartPos -= BurstPos;
                            }
                            else
                            {
                                yield return new WaitForSeconds(TimeBetweenBursts);
                                CurrTime += TimeBetweenBursts;
                            }
                        }
                    }


                }
            }
            yield return new WaitForSeconds(0.1f);
            MainSystem.gameObject.transform.localEulerAngles = Vector3.zero;
        }

        private IEnumerator UniformAnmimator()
        {
            float start = Startanlge(true);
            float pos = Startpos(true);
            if (FanType == DirectionalType.RightToLeft || FanType == DirectionalType.RightZipper)
            {
                start = Startanlge(false);
                pos = Startpos(false);
            }

#if UNITY_EDITOR
            Debug.Log("StartAngle =  " + start);
            Debug.Log("StartPos =  " + pos);
#endif

            yield return new WaitForSeconds(StartDelay);
            CurrTime += StartDelay;

            if (FanType == DirectionalType.LeftZipper || FanType == DirectionalType.RightZipper)
            {
                float CycleModifier = (ZDistance / 2);
                //Debug.Log($"MOD: {CycleModifier}");
                for (int i = 0; i <= NumberOfCycles - 1; i++)
                {
#if UNITY_EDITOR
                    Debug.Log("Cycle: " + i);
#endif
                    Vector3 Cyclepos = new Vector3(MainSystem.gameObject.transform.localPosition.x, MainSystem.transform.localPosition.y, CycleModifier);

                    //Debug.Log(Cyclepos.ToString("f4"));

                    MainSystem.transform.localPosition = Cyclepos;

                    CycleModifier -= CyclePos;
                    //Debug.Log($"MOD2: {CycleModifier}");
                    if (i != 0)
                    {
                        yield return new WaitForSeconds(CycleDelays[i - 1]);
                        CurrTime += CycleDelays[i - 1];
                    }

                    if ((i % 2) == 0)
                    {
                        for (int k = 0; k <= NumberOfBursts - 1; k++)
                        {
                            if (k != 0)
                            {
                                yield return new WaitForSeconds(TimeBetweenBursts);
                                CurrTime += TimeBetweenBursts;
                            }
                            float Angle;
                            Transform T = MainSystem.gameObject.transform;
                            if (FanType == DirectionalType.LeftZipper) { Angle = start - (k * AngleBetweenBurst); }
                            else
                            {
                                Angle = start + (k * AngleBetweenBurst);
                            }
                            var setangle = new Vector3(0, 0, Angle);

                            float PoS;
                            if (FanType == DirectionalType.LeftZipper) { PoS = pos + (k * BurstPos); }
                            else
                            {
                                PoS = pos - (k * BurstPos);
                            }
                            var setpos = new Vector3(PoS, MainSystem.gameObject.transform.localPosition.y, MainSystem.gameObject.transform.localPosition.z);


                            MainSystem.gameObject.transform.localEulerAngles = setangle;
                            MainSystem.gameObject.transform.localPosition = setpos;
                            var emitParams = new ParticleSystem.EmitParams();
                            if (EnableCustomColors)
                            {
                                emitParams.startColor = CycleColors[i];

                            }
                            MainSystem.Emit(emitParams, 1);
#if UNITY_EDITOR
                            Debug.Log("Setting rotation to: " + setangle + " Time: " + CurrTime.ToString("00.00"));
#endif

                        }
                    }
                    else
                    {
                        for (int k = 0; k <= NumberOfBursts - 1; k++)
                        {
                            if (k != 0)
                            {
                                yield return new WaitForSeconds(TimeBetweenBursts);
                                CurrTime += TimeBetweenBursts;
                            }
                            float Angle;
                            Transform T = MainSystem.gameObject.transform;
                            if (FanType == DirectionalType.LeftZipper) { Angle = (start * -1) + (k * AngleBetweenBurst); }
                            else
                            {
                                Angle = (start * -1) - (k * AngleBetweenBurst);
                            }
                            var setangle = new Vector3(0, 0, Angle);

                            float PoS;
                            if (FanType == DirectionalType.LeftZipper) { PoS = -pos - (k * BurstPos); }
                            else
                            {
                                PoS = -pos + (k * BurstPos);
                            }
                            var setpos = new Vector3(PoS, MainSystem.gameObject.transform.localPosition.y, MainSystem.gameObject.transform.localPosition.z);

                            MainSystem.gameObject.transform.localEulerAngles = setangle;
                            MainSystem.gameObject.transform.localPosition = setpos;
                            var emitParams = new ParticleSystem.EmitParams();
                            if (EnableCustomColors)
                            {
                                emitParams.startColor = CycleColors[i];

                            }
                            MainSystem.Emit(emitParams, 1);
#if UNITY_EDITOR
                            Debug.Log("Setting rotation to: " + setangle + " Time: " + CurrTime.ToString("00.00"));
#endif

                        }
                    }


                }
            }

            if (FanType == DirectionalType.LeftToRight || FanType == DirectionalType.RightToLeft)
            {
                float CycleModifier = (ZDistance / 2);

                for (int i = 0; i <= NumberOfCycles - 1; i++)
                {
#if UNITY_EDITOR
                    Debug.Log("Cycle: " + i);
#endif
                    Vector3 Cyclepos = new Vector3(MainSystem.gameObject.transform.localPosition.x, MainSystem.transform.localPosition.y, CycleModifier);

                    MainSystem.transform.localPosition = Cyclepos;
                    CycleModifier -= CyclePos;
                    if (i != 0)
                    {
                        yield return new WaitForSeconds(CycleDelays[i - 1]);
                        CurrTime += CycleDelays[i - 1];
                    }

                    for (int k = 0; k <= NumberOfBursts - 1; k++)
                    {
                        if (k != 0)
                        {
                            yield return new WaitForSeconds(TimeBetweenBursts);
                            CurrTime += TimeBetweenBursts;
                        }
                        float Angle;
                        Transform T = MainSystem.gameObject.transform;
                        if (FanType == DirectionalType.LeftToRight) { Angle = start - (k * AngleBetweenBurst); }
                        else
                        {
                            Angle = start + (k * AngleBetweenBurst);
                        }
                        var setangle = new Vector3(0, 0, Angle);

                        float PoS;
                        if (FanType == DirectionalType.LeftToRight) { PoS = pos + (k * BurstPos); }
                        else
                        {
                            PoS = pos - (k * BurstPos);
                        }
                        var setpos = new Vector3(PoS, MainSystem.gameObject.transform.localPosition.y, MainSystem.gameObject.transform.localPosition.z);

                        MainSystem.gameObject.transform.localEulerAngles = setangle;
                        MainSystem.gameObject.transform.localPosition = setpos;
                        var emitParams = new ParticleSystem.EmitParams();
                        if (EnableCustomColors)
                        {
                            emitParams.startColor = CycleColors[i];
                        }
                        MainSystem.Emit(emitParams, 1);

#if UNITY_EDITOR
                        Debug.Log("Setting rotation to: " + setangle + " Time: " + CurrTime.ToString("00.00"));
#endif

                    }

                }
            }
            yield return new WaitForSeconds(0.1f);
            MainSystem.gameObject.transform.localEulerAngles = Vector3.zero;
            var setpos2 = new Vector3(0, MainSystem.gameObject.transform.localPosition.y, 0);
            MainSystem.gameObject.transform.localPosition = setpos2;
        }


        public float Startanlge(bool left)
        {
            float Angle;
            if (NumberOfBursts % 2 == 0f)
            {
                Angle = ((NumberOfBursts / 2f) * AngleBetweenBurst) - (AngleBetweenBurst / 2);
            }
            else
            {
                Angle = ((NumberOfBursts / 2f) - 0.5f) * AngleBetweenBurst;
            }

            if (!left)
            {
                Angle = Angle * -1.0f;
            }
            return Angle;
        }

        public float Startpos(bool left)
        {
            float Angle;
            //Debug.Log($"STARTPOS: {NumberOfBursts}");
            //Debug.Log($"STARTPOS: {BurstPos}");
            if (NumberOfBursts % 2 == 0f)
            {
                Angle = ((NumberOfBursts / 2f) * BurstPos) - (BurstPos / 2);
            }
            else
            {
                Angle = ((NumberOfBursts / 2f) - 0.5f) * BurstPos;
            }

            if (left)
            {
                Angle = Angle * -1.0f;
            }
            return Angle;
        }

        public float StartanlgePyramid()
        {
            float Angle;
            if (NumberOfBursts % 2 == 0f)
            {
                Angle = (AngleBetweenBurst / 2f);
            }
            else
            {
                Angle = 0f;
            }
            return Angle;
        }

        public float StartPosPyramid()
        {
            float Angle;
            if (NumberOfBursts % 2 == 0f)
            {
                Angle = (BurstPos / 2f);
            }
            else
            {
                Angle = 0f;
            }
            return Angle;
        }



        public void SetCycles()
        {
            if (EnableCustomDelays == false)
            {
                CycleDelays.Clear();
                for (int i = 0; i < NumberOfCycles - 1; i++)
                {
                    CycleDelays.Add(TimeBetweenCycles);
                }
            }
        }

        public void SetColors()
        {
            if (EnableCustomColors == true)
            {
                CycleColors.Clear();
                for (int i = 0; i < NumberOfCycles; i++)
                {
                    CycleColors.Add(MainSystem.main.startColor.color);
                }
            }
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (MainSystem != null)
            {
                Vector3 pos = MainSystem.gameObject.transform.position;
                Vector3 Xmin = new Vector3(pos.x - (XDistance / 2), pos.y, pos.z);
                Vector3 Xmax = new Vector3(pos.x + (XDistance / 2), pos.y, pos.z);
                Vector3 Zmin = new Vector3(0, pos.y, pos.z - (ZDistance / 2));
                Vector3 Zmax = new Vector3(0, pos.y, pos.z + (ZDistance / 2));


                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(Xmin, 0.025f);
                Gizmos.DrawSphere(Xmax, 0.025f);
                Gizmos.DrawLine(Xmin, Xmax);
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(Zmin, 0.025f);
                Gizmos.DrawSphere(Zmax, 0.025f);
                Gizmos.DrawLine(Zmin, Zmax);

            }

        }
#endif
    }
}
