using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.SocialPlatforms;
using Unity.XR.CoreUtils;

public class Score_A : MonoBehaviour
{
    // public
    public bool normalMode;
    public bool gameClear;
    public bool disturber;

    public float partMistakePercentage = 0.003f;
    public float partMistakeCooltime = 6f;
    public float disturberPercentage = 0.001f;
    public float disturberCooltime = 15f;
    
    public GameObject disturberObj1;
    public GameObject disturberObj2;
    public GameObject bulletPrefab;

    public Text UI_GameTime;
    public Slider UI_Progress;
    public Text UI_Tempo;
    public Text UI_Dynamic;
    public Text UI_State;
    public Text UI_Score;
    public Text UI_WarningMessage;

    // private
    private enum Tempo { Largo, Adagio, Moderato, Vivace, Presto, End }
    private enum Dynamic { pppp, ppp, pp, p, mp, mf, f, ff, fff, ffff, End }
    private enum Inst { Violin, Viola, Cello, Bass, Wood, Brass, Percussion, End }
    private enum State { Poor, Bad, Great, Perfect, End }

    private float gameTime;

    private double progress;
    private Tempo tempo;
    private Dynamic dynamic;
    private State state;
    private float timer;
    private float scoreTimer;
    private int score;

    private GameObject audioCheeringObj;
    private GameObject audioBooingObj;
    private GameObject[,] audioObj;
    private AudioSource[,] audioSource;
    private bool[] instActivate;
    private GameObject[] effectObj;

    private Queue<Inst> partMistake;
    private bool[] disturb;
    private float[] coolTime;
    private float warningTime;

    public const float finishPoint = 0.93f;

    public const int perfectScore = 4;
    public const int greatScore = 2;
    public const int badScore = -1;
    public const int poorScore = -3;

    public const float increaseTime = 3f;
    public const float decreaseTime = 1f;

    void Start()
    {
        gameClear = false;
        gameTime = 0f;

        progress = 0d;
        tempo = Tempo.Moderato;
        dynamic = Dynamic.mp;
        state = State.Great;
        timer = 0f;
        score = 0;
        warningTime = 0f;

        audioObj = new GameObject[(int)Tempo.End, (int)Inst.End];
        audioSource = new AudioSource[(int)Tempo.End, (int)Inst.End];
        instActivate = new bool[(int)Inst.End];
        effectObj = new GameObject[(int)Inst.End];

        InitializeAudioObj();
        InitializeAudioSource();
        InitializeInstActivate();
        InitializeContents();

        SetChannelActivate(tempo, true);
        SetChannelPlay(tempo, true);

        InitializeEffectObj();
    }

    void Update()
    {
        UpdateState();

        if (!normalMode)
        {
            MusicError();
        }

        KeyInput();
        UpdateEffect();
        UpdateUI();
    }

    void UpdateState()
    {
        gameTime += Time.deltaTime;

        progress = (double)audioSource[(int)tempo, 0].time / (double)audioSource[(int)tempo, 0].clip.length + 0.0001f;

        if (finishPoint <= progress)
        {
            if (!gameClear
                && score >= 50f)
            {
                audioCheeringObj.GetComponent<AudioSource>().Play();
                audioCheeringObj.GetComponent<AudioSource>().time = 3f;
            }
            else if (!gameClear
                && score < 50f)
            {
                audioBooingObj.GetComponent<AudioSource>().Play();
            }

            gameClear = true;
        }

        if (gameClear)
        {
            return;
        }

        scoreTimer += Time.deltaTime;
        if (1f <= scoreTimer)
        {
            switch (state)
            {
                case State.Perfect:
                    score += perfectScore;
                    break;
                case State.Great:
                    score += greatScore;
                    break;
                case State.Bad:
                    if (decreaseTime <= timer)
                    {
                        score += badScore;
                    }
                    break;
                case State.Poor:
                    if (decreaseTime <= timer)
                    {
                        score += poorScore;
                    }
                    break;
            }

            score = (0 > score) ? 0 : score;
            scoreTimer = 0f;
        }

        timer += Time.deltaTime;
        if (State.Great == state && increaseTime <= timer)
        {
            state = State.Perfect;
            timer = 0f;
        }
    }

    void ErrorCheck()
    {
        // partMistake check
        if (0 == partMistake.Count)
        {
            disturb[0] = false;
        }

        // Tempo error check
        if (disturb[1]
            && (Tempo.Adagio <= tempo && Tempo.Vivace >= tempo))
        {
            disturb[1] = false;
        }

        // Dynamic error check
        if (disturb[2]
            && (Dynamic.pp <= dynamic && Dynamic.ff >= dynamic))
        {
            disturb[2] = false;
        }

        // Set State
        int count = 0;

        for (int i = 0; i < disturb.Length; ++i)
        {
            if (disturb[i])
            {
                ++count;
            }
        }

        switch (count)
        {
            case 3:
            case 2:
                if (decreaseTime <= timer)
                {
                    state = State.Poor;
                }
                break;
            case 1:
                if (decreaseTime <= timer)
                {
                    state = State.Bad;
                }
                break;
            case 0:
                if (State.Great > state)
                {
                    state = State.Great;
                    timer = 0f;
                }
                break;
        }
    }

    void PartMistake()
    {
        bool canMistake = false;
        for (int i = 0; i < (int)Inst.End; ++i)
        {
            if (instActivate[i])
                canMistake = true;
        }

        if (!canMistake)
        {
            return;
        }

        if (State.Great <= state)
        {
            timer = 0f;
        }

        disturb[0] = true;
        Inst randomPart;
        do { randomPart = (Inst)UnityEngine.Random.Range(0, (int)Inst.End); } while (!instActivate[(int)randomPart]);
        instActivate[(int)randomPart] = false;
        SetInstActivate(tempo, randomPart);
        partMistake.Enqueue(randomPart);
    }

    void Disturber()
    {
        if (State.Great <= state)
        {
            timer = 0f;
        }

        disturber = true;

        bool t = false; // tempo
        bool d = false; // dynamic
        if (0.5f > UnityEngine.Random.Range(0f, 1f))
        {
            if (0.5f > UnityEngine.Random.Range(0f, 1f))
            {
                t = true;
            }
            else
            {
                d = true;
            }
        }
        else
        {
            t = true;
            d = true;
        }

        // Tempo
        if (t)
        {
            disturb[1] = true;

            if (0 == tempo)
            {
                SetChannelPlay(tempo, false);
                tempo = Tempo.End - 1;
                SetChannelPlay(tempo, true, true);
            }
            else if (Tempo.End - 1 == tempo)
            {
                SetChannelPlay(tempo, false);
                tempo = 0;
                SetChannelPlay(tempo, true);
            }
            else if (0.5f > UnityEngine.Random.Range(0f, 1f))
            {
                SetChannelPlay(tempo, false);
                tempo = 0;
                SetChannelPlay(tempo, true);
            }
            else
            {
                SetChannelPlay(tempo, false);
                tempo = Tempo.End - 1;
                SetChannelPlay(tempo, true, true);
            }
        }

        // Dynamic
        if (d)
        {
            disturb[2] = true;

            if (Dynamic.pppp == dynamic)
            {
                dynamic = Dynamic.ffff;
            }
            else if (Dynamic.ffff == dynamic)
            {
                dynamic = Dynamic.pppp;
            }
            else if (0.5f > UnityEngine.Random.Range(0f, 1f))
            {
                dynamic = Dynamic.pppp;
            }
            else
            {
                dynamic = Dynamic.ffff;
            }
            SetChannelVolume(tempo);
        }
    }

    void MusicError()
    {
        if (gameClear)
        {
            return;
        }

        ErrorCheck();

        for (int i = 0; i < coolTime.Length; ++i)
        {
            coolTime[i] = (0f < coolTime[i] - Time.deltaTime) ? coolTime[i] - Time.deltaTime : 0f;
        }

        // PartMistake
        if (0f == coolTime[0])
        {
            if (partMistakePercentage > UnityEngine.Random.Range(0f, 1f))
            {
                PartMistake();
                coolTime[0] = partMistakeCooltime;
            }
        }

        // Disturber
        if (0f == coolTime[1]
            && !disturber
            && (!disturb[1] && !disturb[2]))
        {
            if (disturberPercentage > UnityEngine.Random.Range(0f, 1f))
            {
                Disturber();
                coolTime[1] = disturberCooltime;
            }
        }
    }

    void SetChannelPlay(Tempo tempo, bool play, bool faster = false)
    {
        for (int i = 0; i < (int)Inst.End; ++i)
        {
            if (play)
            {
                float bias = faster ? -0.5f * (float)progress : 1f * (float)progress;

                audioSource[(int)tempo, i].time = (float)(progress * (double)audioSource[(int)tempo, 0].clip.length) + 0.0001f + bias;
                audioSource[(int)tempo, i].Play();
                ChannelActivate(tempo);
            }
            else
            {
                audioSource[(int)tempo, i].Stop();
            }
        }
    }

    void SetChannelActivate(Tempo tempo, bool activate)
    {
        for (int i = 0; i < (int)Inst.End; ++i)
        {
            instActivate[i] = activate;
            SetInstActivate(tempo, (Inst)i);
        }
    }

    void ChannelActivate(Tempo tempo)
    {
        for (int i = 0; i < (int)Inst.End; ++i)
        {
            SetInstActivate(tempo, (Inst)i);
        }
    }

    void SetInstActivate(Tempo tempo, Inst inst)
    {
        float volume = (instActivate[(int)inst]) ? calculateDynamic() : 0f;
        audioSource[(int)tempo, (int)inst].volume = volume;
    }

    void SetChannelVolume(Tempo tempo)
    {
        for (int i = 0; i < (int)Inst.End; ++i)
        {
            if (instActivate[i])
            {
                audioSource[(int)tempo, i].volume = calculateDynamic();
            }
        }
    }

    void KeyInput()
    {
        // Tempo Control
        if (Input.GetKeyDown(KeyCode.W)
            && Tempo.End - 1 > tempo)
        {
            IncreaseTempo();
        }
        if (Input.GetKeyDown(KeyCode.Q)
            && Tempo.Largo < tempo)
        {
            DecreaseTempo();
        }

        // Dynamic Control
        if (Input.GetKeyDown(KeyCode.R))
        {
            IncreaseDynamic();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            DecreaseDynamic();
        }

        // disturber
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RemoveDisturber();
        }

        // partMistake Control
        if (Input.GetKeyDown(KeyCode.P))
        {
            RecoverPartMistake();
        }
    }

    public void IncreaseTempo()
    {
        if (gameClear)
        {
            return;
        }

        if (disturber)
        {
            if (warningTime == 0f)
            {
                warningTime = 2f;
            }

            return;
        }

        SetChannelPlay(tempo, false);
        tempo = (Tempo.End == ++tempo) ? Tempo.End - 1 : tempo;
        SetChannelPlay(tempo, true, true);
    }

    public void DecreaseTempo()
    {
        if (gameClear)
        {
            return;
        }

        if (disturber)
        {
            if (warningTime == 0f)
            {
                warningTime = 2f;
            }

            return;
        }

        SetChannelPlay(tempo, false);
        tempo = (0 > --tempo) ? Tempo.Largo : tempo;
        SetChannelPlay(tempo, true);
    }

    public void IncreaseDynamic()
    {
        if (gameClear)
        {
            return;
        }

        if (disturber)
        {
            if (warningTime == 0f)
            {
                warningTime = 2f;
            }

            return;
        }

        dynamic = (Dynamic.End == ++dynamic) ? Dynamic.ffff : dynamic;
        SetChannelVolume(tempo);
    }

    public void DecreaseDynamic()
    {
        if (gameClear)
        {
            return;
        }

        if (disturber)
        {
            if (warningTime == 0f)
            {
                warningTime = 2f;
            }

            return;
        }

        dynamic = (0 > --dynamic) ? Dynamic.pppp : dynamic;
        SetChannelVolume(tempo);
    }

    public void RemoveDisturber()
    {
        if (gameClear)
        {
            return;
        }

        if (!disturber)
        {
            return;
        }

        bool oneDisturb = false;
        if (disturb[1] && disturb[2])
        {
            oneDisturb = false;
        }
        else if (disturb[1] || disturb[2])
        {
            oneDisturb = true;
        }

        GameObject disturberObj = (oneDisturb) ? disturberObj1 : disturberObj2;

        // Bullet 持失
        GameObject bulletObj = Instantiate(bulletPrefab);
        bulletObj.GetComponent<Bullet>().InitializeBullet(
            0,
            new Vector3(0f, 2f, -0.6f),
            disturberObj.GetComponent<Transform>().position,
            disturberObj.GetComponent<SphereCollider>());
    }

    public void RecoverPartMistake()
    {
        if (gameClear)
        {
            return;
        }

        if (disturber)
        {
            if (warningTime == 0f)
            {
                warningTime = 2f;
            }

            return;
        }

        if (0 == partMistake.Count)
        {
            return;
        }

        Inst instrument = partMistake.Peek();
        GameObject instObj = effectObj[(int)instrument];

        // Bullet 持失
        GameObject bulletObj;
        if (Inst.Violin != instrument)
        {
            bulletObj = Instantiate(bulletPrefab);
            bulletObj.GetComponent<Bullet>().InitializeBullet(
                1,
                new Vector3(0f, 2f, -0.6f),
                instObj.GetComponent<Transform>().position,
                instObj.GetComponent<SphereCollider>());
        }
        else
        {
            bulletObj = Instantiate(bulletPrefab);
            bulletObj.GetComponent<Bullet>().InitializeBullet(
                1,
                new Vector3(0f, 2f, -0.6f),
                instObj.GetComponent<Transform>().Find("Effect_Violin_L").position,
                instObj.GetComponent<Transform>().Find("Effect_Violin_L").gameObject.GetComponent<SphereCollider>());

            bulletObj = Instantiate(bulletPrefab);
            bulletObj.GetComponent<Bullet>().InitializeBullet(
                2,
                new Vector3(0f, 2f, -0.6f),
                instObj.GetComponent<Transform>().Find("Effect_Violin_R").position,
                instObj.GetComponent<Transform>().Find("Effect_Violin_R").gameObject.GetComponent<SphereCollider>());
        }
    }

    public void RemoveDisturberComplete()
    {
        disturber = false;
    }

    public void RecoverPartMistakeComplete()
    {
        if (0 < partMistake.Count)
        {
            Inst part = partMistake.Dequeue();
            instActivate[(int)part] = true;
            SetInstActivate(tempo, part);
        }
    }

    float calculateDynamic()
    {
        float volume = 0f;

        switch (dynamic)
        {
            case Dynamic.pppp:
                volume = 0.1f;
                break;
            case Dynamic.ppp:
                volume = 0.2f;
                break;
            case Dynamic.pp:
                volume = 0.3f;
                break;
            case Dynamic.p:
                volume = 0.4f;
                break;
            case Dynamic.mp:
                volume = 0.5f;
                break;
            case Dynamic.mf:
                volume = 0.6f;
                break;
            case Dynamic.f:
                volume = 0.7f;
                break;
            case Dynamic.ff:
                volume = 0.8f;
                break;
            case Dynamic.fff:
                volume = 0.9f;
                break;
            case Dynamic.ffff:
                volume = 1f;
                break;
        }

        return volume;
    }

    void UpdateEffect()
    {
        for (int i = 0; i < (int)Inst.End; ++i)
        {
            effectObj[i].SetActive(instActivate[i] ? false : true);
        }

        bool oneDisturb = false;
        bool twoDisturb = false;
        if (disturber
            && (disturb[1] && disturb[2]))
        {
            oneDisturb = false;
            twoDisturb = true;
        }
        else if (disturber
            && (disturb[1] || disturb[2]))
        {
            oneDisturb = true;
            twoDisturb = false;
        }
        disturberObj1.SetActive(oneDisturb);
        disturberObj2.SetActive(twoDisturb);
    }

    void UpdateUI()
    {
        UI_Score.enabled = gameClear;
        UI_State.enabled = gameClear;

        UI_GameTime.text = ((int)gameTime).ToString();

        UI_Progress.value = (float)progress;
        UI_Tempo.text = tempo.ToString();
        UI_Dynamic.text = dynamic.ToString();
        UI_State.text = state.ToString();
        UI_Score.text = score.ToString();

        UI_Tempo.color = new Color(disturb[1] ? 1f : 0f, 0f, 0f);
        UI_Dynamic.color = new Color(disturb[2] ? 1f : 0f, 0f, 0f);

        float warningAlpha = 0f;
        if (warningTime > 0f)
        {
            warningTime -= Time.deltaTime;
            if (warningTime > 1f)
            {
                warningAlpha = 1f - (warningTime - 1f);
            }
            else
            {
                warningAlpha = warningTime;
            }
        }
        else
        {
            warningTime = 0f;
        }
        UI_WarningMessage.color = new Color(1f, 1f, 1f, warningAlpha);

        if (!normalMode
            && gameClear
            && score >= 100)
        {
            UI_State.text = "Perfect";
        }
        else if (!normalMode
            && gameClear
            && score >= 50)
        {
            UI_State.text = "Great";
        }
        else if (!normalMode
            && gameClear
            && score >= 10)
        {
            UI_State.text = "Bad";
        }
        else if (!normalMode
            && gameClear)
        {
            UI_State.text = "Poor";
        }
    }

    void InitializeAudioObj()
    {
        // Init AudioObj Largo
        audioObj[(int)Tempo.Largo, (int)Inst.Violin] = GameObject.Find("58_Violin");
        audioObj[(int)Tempo.Largo, (int)Inst.Viola] = GameObject.Find("58_Viola");
        audioObj[(int)Tempo.Largo, (int)Inst.Cello] = GameObject.Find("58_Cello");
        audioObj[(int)Tempo.Largo, (int)Inst.Bass] = GameObject.Find("58_Bass");
        audioObj[(int)Tempo.Largo, (int)Inst.Wood] = GameObject.Find("58_Wood");
        audioObj[(int)Tempo.Largo, (int)Inst.Brass] = GameObject.Find("58_Brass");
        audioObj[(int)Tempo.Largo, (int)Inst.Percussion] = GameObject.Find("58_Percussion");

        // Init AudioObj Adagio
        audioObj[(int)Tempo.Adagio, (int)Inst.Violin] = GameObject.Find("88_Violin");
        audioObj[(int)Tempo.Adagio, (int)Inst.Viola] = GameObject.Find("88_Viola");
        audioObj[(int)Tempo.Adagio, (int)Inst.Cello] = GameObject.Find("88_Cello");
        audioObj[(int)Tempo.Adagio, (int)Inst.Bass] = GameObject.Find("88_Bass");
        audioObj[(int)Tempo.Adagio, (int)Inst.Wood] = GameObject.Find("88_Wood");
        audioObj[(int)Tempo.Adagio, (int)Inst.Brass] = GameObject.Find("88_Brass");
        audioObj[(int)Tempo.Adagio, (int)Inst.Percussion] = GameObject.Find("88_Percussion");

        // Init AudioObj Moderato
        audioObj[(int)Tempo.Moderato, (int)Inst.Violin] = GameObject.Find("118_Violin");
        audioObj[(int)Tempo.Moderato, (int)Inst.Viola] = GameObject.Find("118_Viola");
        audioObj[(int)Tempo.Moderato, (int)Inst.Cello] = GameObject.Find("118_Cello");
        audioObj[(int)Tempo.Moderato, (int)Inst.Bass] = GameObject.Find("118_Bass");
        audioObj[(int)Tempo.Moderato, (int)Inst.Wood] = GameObject.Find("118_Wood");
        audioObj[(int)Tempo.Moderato, (int)Inst.Brass] = GameObject.Find("118_Brass");
        audioObj[(int)Tempo.Moderato, (int)Inst.Percussion] = GameObject.Find("118_Percussion");

        // Init AudioObj Vivace
        audioObj[(int)Tempo.Vivace, (int)Inst.Violin] = GameObject.Find("148_Violin");
        audioObj[(int)Tempo.Vivace, (int)Inst.Viola] = GameObject.Find("148_Viola");
        audioObj[(int)Tempo.Vivace, (int)Inst.Cello] = GameObject.Find("148_Cello");
        audioObj[(int)Tempo.Vivace, (int)Inst.Bass] = GameObject.Find("148_Bass");
        audioObj[(int)Tempo.Vivace, (int)Inst.Wood] = GameObject.Find("148_Wood");
        audioObj[(int)Tempo.Vivace, (int)Inst.Brass] = GameObject.Find("148_Brass");
        audioObj[(int)Tempo.Vivace, (int)Inst.Percussion] = GameObject.Find("148_Percussion");

        // Init AudioObj Presto
        audioObj[(int)Tempo.Presto, (int)Inst.Violin] = GameObject.Find("178_Violin");
        audioObj[(int)Tempo.Presto, (int)Inst.Viola] = GameObject.Find("178_Viola");
        audioObj[(int)Tempo.Presto, (int)Inst.Cello] = GameObject.Find("178_Cello");
        audioObj[(int)Tempo.Presto, (int)Inst.Bass] = GameObject.Find("178_Bass");
        audioObj[(int)Tempo.Presto, (int)Inst.Wood] = GameObject.Find("178_Wood");
        audioObj[(int)Tempo.Presto, (int)Inst.Brass] = GameObject.Find("178_Brass");
        audioObj[(int)Tempo.Presto, (int)Inst.Percussion] = GameObject.Find("178_Percussion");

        audioCheeringObj = GameObject.Find("Audio_Cheering");
        audioBooingObj = GameObject.Find("Audio_Booing");
    }

    void InitializeAudioSource()
    {
        for (int i = 0; i < (int)Tempo.End; ++i)
        {
            for (int j = 0; j < (int)Inst.End; ++j)
            {
                audioSource[i, j] = audioObj[i, j].GetComponent<AudioSource>();
            }
        }
    }

    void InitializeInstActivate()
    {
        for (int i = 0; i < (int)Tempo.End; ++i)
        {
            for (int j = 0; j < (int)Inst.End; ++j)
            {
                instActivate[j] = false;
                SetInstActivate((Tempo)i, (Inst)j);
            }
        }
    }

    void InitializeContents()
    {
        partMistake = new Queue<Inst>();
        disturb = new bool[3];
        coolTime = new float[2];

        partMistake.Clear();

        disturber = false;

        for (int i = 0; i < disturb.Length; ++i)
        {
            disturb[i] = false;
        }

        coolTime[0] = partMistakeCooltime;
        coolTime[1] = disturberCooltime;
    }

    void InitializeEffectObj()
    {
        // Init effectObj
        effectObj[(int)Inst.Violin] = GameObject.Find("Effect_Violin");
        effectObj[(int)Inst.Viola] = GameObject.Find("Effect_Viola");
        effectObj[(int)Inst.Cello] = GameObject.Find("Effect_Cello");
        effectObj[(int)Inst.Bass] = GameObject.Find("Effect_Bass");
        effectObj[(int)Inst.Wood] = GameObject.Find("Effect_Wood");
        effectObj[(int)Inst.Brass] = GameObject.Find("Effect_Brass");
        effectObj[(int)Inst.Percussion] = GameObject.Find("Effect_Percussion");
    }
}
