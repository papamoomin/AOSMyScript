using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워의 투사체를 담당하는 스크립트
/// </summary>
public class TowerProjectile : MonoBehaviour
{
    public Transform target;
    public List<GameObject> trails;
    public List<GameObject> childEffect;
    public GameObject muzzlePrefab;

    private GAP_ParticleSystemController.ParticleSystemController customPsSystem;
    private GameObject muzzleGO;
    private Vector3 offset;
    private bool isFirstShot = false;
    private bool isHasParent = false;

    void Awake()
    {
        InitializeEffect();
    }

    private void Update()
    {
        if (target != null)
        {
            Vector3 dir = target.transform.position - transform.position;
            transform.position += dir.normalized * Time.deltaTime * 40f;
            transform.localScale = Vector3.zero;

            // 포탄이 타겟에게 도달하면 활성 상태를 끈다
            if (dir.magnitude < 0.5f)
            {
                CancelInvoke();
                ActiveFalse();
            }
        }
    }

    private void OnEnable()
    {
        ResetEffect();

        if (!isHasParent)
        {
            if (transform.parent == null)
                isHasParent = false;
            else
                isHasParent = true;
        }

        if (isHasParent)
            transform.position = transform.parent.position;
    }

    private void OnDisable()
    {
        if (!isHasParent)
        {
            if (transform.parent == null)
                isHasParent = false;
            else
                isHasParent = true;
        }

        if (isHasParent)
            transform.position = transform.parent.position;

        target = null;
    }

    /// <summary>
    /// 이펙트를 초기화하는 함수
    /// </summary>
    private void InitializeEffect()
    {
        if (childEffect == null && childEffect.Count < 0)
        {
            childEffect = new List<GameObject>();

            for (int i = 0; i < transform.childCount; i++)
                childEffect.Add(transform.GetChild(i).gameObject);
        }

        if (GetComponent<GAP_ParticleSystemController.ParticleSystemController>())
            customPsSystem = GetComponent<GAP_ParticleSystemController.ParticleSystemController>();
    }

    /// <summary>
    /// 이펙트를 리셋하는 함수
    /// </summary>
    private void ResetEffect()
    {
        if (isFirstShot)
        {
            transform.position = Vector3.zero;
            GAP_ParticleSystemController.ParticleSystemController customPsSystem = GetComponent<GAP_ParticleSystemController.ParticleSystemController>();

            if (customPsSystem != null)
            {
                for (int i = 0; i < customPsSystem.ParticleSystems.Count; i++)
                {
                    customPsSystem.ParticleSystems[i].transform.position = Vector3.zero;
                    customPsSystem.ParticleSystems[i].gameObject.SetActive(true);

                    if (customPsSystem.ParticleSystems[i].GetComponent<ParticleSystem>() != null)
                        customPsSystem.ParticleSystems[i].GetComponent<ParticleSystem>().Play();
                }
            }
            else
            {
                for (int i = 0; i < childEffect.Count; i++)
                {
                    childEffect[i].SetActive(true);

                    if (childEffect[i].GetComponent<ParticleSystem>() != null)
                        childEffect[i].GetComponent<ParticleSystem>().Play();
                }
            }
        }

        if (muzzlePrefab != null)
        {
            if (muzzleGO == null)
                muzzleGO = Instantiate(muzzlePrefab, transform.position, Quaternion.identity, transform);

            muzzleGO.SetActive(true);
            muzzleGO.transform.forward = gameObject.transform.forward + offset;
            ParticleSystem particleSys = muzzleGO.GetComponent<ParticleSystem>();

            if (particleSys != null)
                particleSys.Play();
            else
            {
                ParticleSystem psChild = muzzleGO.transform.GetChild(0).GetComponent<ParticleSystem>();
                psChild.Play();
            }
        }
    }

    /// <summary>
    /// 일정 시간이 지난 후 활성 상태를 꺼주는 함수
    /// </summary>
    /// <param name="time">몇 초 뒤에 꺼질 것인가</param>
    public void ActiveFalse(float time)
    {
        Invoke("ActiveFalse", time);
    }

    /// <summary>
    /// 활성 상태를 꺼주는 함수
    /// </summary>
    private void ActiveFalse()
    {
        gameObject.SetActive(false);
    }
}