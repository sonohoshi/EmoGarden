﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GardenEelHead : GardenEelBody
{
    public GameObject body;
    public GameObject eye;
    public Sprite idleEye;
    public Sprite stunedEye;
    public PolygonCollider2D polygon;

    private int _level = 0;
    private float _lengthOfBody;
    private bool _canMove = true;
    private Transform _tail;
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _eyeSpriteRenderer;
    private List<GardenEelBody> _bodyList;

    private const float StartSize = .5f;
    private const float StartLength = 5f;
    private const int StartCountOfBody = 7;
    private static readonly Color newColor = new Color32(100, 250, 220, 255);
    private static readonly Vector2 midPosition = new Vector2(0, 6);

    private static readonly float[] LengthArray = new[] {1f, 1.9f, 3.5f, 4.5f, 5f};
    private static readonly float[] SizeArray = new[] {1f, 1.4f, 2f, 2.6f, 3.2f};
    private static readonly int[] ObjectCountArray = new[] {7, 15, 20, 21, 21};

    public void LevelUp(int level)
    {
        int size = _bodyList.Count - 1;
        float before = _lengthOfBody;
        
        _lengthOfBody = StartLength * LengthArray[level];

        foreach (var bodys in _bodyList)
        {
            bodys.transform.localScale = Vector2.one * StartSize * SizeArray[level];
            bodys.spaceOfBody = GetComponent<CircleCollider2D>().radius * SizeArray[level];
        }
        
        Debug.Log($"level : {level}, bodyCount : {_bodyList.Count}");
        
        AddBody(ObjectCountArray[level] - _bodyList.Count - 1, SizeArray[level]);

        if (level == 2)
        {
            foreach (var bodys in _bodyList)
            {
                bodys.GetComponent<SpriteRenderer>().DOColor(newColor, 1f);
            }
        }

        _level = level;
    }
    
    protected override void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _eyeSpriteRenderer = eye.GetComponent<SpriteRenderer>();

        _bodyList = new List<GardenEelBody>();
        _bodyList.Add(this);

        _lengthOfBody = StartLength;
        transform.localScale = Vector3.one * StartSize;

        AddBody(StartCountOfBody, SizeArray[_level]);
    }

    protected override void FixedUpdate()
    {
        _target = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        var velocity = _rigidbody2D.velocity;
            
        if (velocity.y < -1f)
        {
            velocity.y = -1f;
            _rigidbody2D.velocity = velocity;
        }

        if (!CheckPlayerPosition())
        {
            var dir = midPosition - (Vector2) transform.position;
            _rigidbody2D.AddForce(dir.normalized);
            StartCoroutine(ResetVelocity());
        }
        
        if ((Input.GetMouseButton(0) || Input.touchCount > 0) && _canMove)
        {
            MoveToTarget(_target);
        }
    }

    private void AddBody(int n, float sizeValue)
    {
        if (n <= 0)
        {
            return;
        }
        
        int size = _bodyList.Count - 1;
        _bodyList[_bodyList.Count - 1].SetIsTail(false);
        
        for (int i = 0; i < n; i++)
        {
            var obj = Instantiate(body, new Vector3(0, -6.5f, 0), Quaternion.identity);
            var bodyComponent = obj.GetComponent<GardenEelBody>();
            bodyComponent.parent = _bodyList[size + i].transform;
            bodyComponent.spaceOfBody = GetComponent<CircleCollider2D>().radius * SizeArray[_level];
            _tail = obj.transform;
            _tail.localScale *= sizeValue;
            _bodyList.Add(bodyComponent);
        }

        for (int i = _bodyList.Count - 2; i >= 0; i--)
        {
            _bodyList[i].child = _bodyList[i + 1].transform;
        }

        _bodyList[_bodyList.Count - 1].SetIsTail(true);
    }

    protected override bool MoveToTarget(Vector3 target)
    {
        Vector2 tailDir = _tail.position - transform.position;

        float magnitude = tailDir.magnitude;

        if (magnitude < _lengthOfBody)
        {
            Vector2 dir = target - transform.position;
            var usingFlip = transform.position - target;
            
            var scale = transform.localScale;
            if (usingFlip.x > 0)
            {
                scale.x = -.5f * SizeArray[_level];
            }
            else
            {
                scale.x = .5f * SizeArray[_level];
            }
            transform.localScale = scale;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            var rotationVector = rotation.eulerAngles + new Vector3(0, 0, -90);
            transform.rotation = Quaternion.Euler(rotationVector);

            _rigidbody2D.velocity = Vector2.zero;
            transform.Translate(Vector2.up * Time.deltaTime * speed);
            return true;
        }
        StartCoroutine(StunPlayer(.5f));

        return false;
    }

    private IEnumerator StunPlayer(float time)
    {
        SoundManager.instance.PlayEffectSound("Stun_Effect");
        _canMove = false;
        _eyeSpriteRenderer.sprite = stunedEye;
        Debug.Log($"{time}초 동안 스턴걸림");
        yield return new WaitForSeconds(time);
        _eyeSpriteRenderer.sprite = idleEye;
        _canMove = true;
    }

    private IEnumerator ResetVelocity()
    {
        yield return new WaitForSeconds(.5f);
        _rigidbody2D.velocity = Vector2.zero;
    }

    private bool CheckPlayerPosition()
    {
        float x = polygon.GetPath(0)[0].x;
        float maxY = polygon.GetPath(0)[0].y;
        float minY = polygon.GetPath(0)[3].y;

        var pos = transform.position;

        return (Mathf.Abs(pos.x) < x) && (pos.y < maxY && pos.y > minY);
    }
}
