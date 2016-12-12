﻿using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class RepeatedGameObject : MonoBehaviour
{
    public GameObject prefab;
    public GameObject blank;
    public bool drawBlank=false;
    public bool flippingX, flippingY;
    public Vector2 size;
    public Vector2 sizeBlank;
    public Vector2 pivotOffset,pivot;

    [Range(1,2000)] public float blankLenght;
    public bool autoSizeBySpriteRenderer = true;
    public bool autoRefresh = true;

    public Sprite defaultSprite, currentSprite;

    [System.Serializable]
    public class Clones
    {
        public int left, right, up, down;
        public bool isSameAs(Clones c)
        {
            if (c == null) return false;
            return left == c.left &&
                right == c.right &&
                up == c.up &&
                down == c.down;
        }
        public void set(Clones c)
        {
            if (c == null) c = new Clones();
            left = c.left;
            right = c.right;
            up = c.up;
            down = c.down;
        }
    }
    public Clones clones;
	public Vector2[] exceptions;


    Clones recentClones;
    Vector3 recentScale;
    Transform tr;
    Transform parent;

    public void Awake()
    {
        tr = GetComponent<Transform>();
        recentScale = Vector3.zero;
        recentClones = new Clones();

        if(autoSizeBySpriteRenderer)
        {
            if (blank)
            {
                sizeBlank = getSize(blank);
            }
            if (prefab)
            {
                name = prefab.name;
                size = getSize(prefab);
            }
            
        }
    }

    public void Start()
    {
        setHideFlags(true);
        //rebuild();
    }

    Vector2 getSize(GameObject pref)
    {
        Vector2 s;
        GameObject p = Instantiate(pref) as GameObject;
        SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
        if (!sr) sr = p.GetComponentInChildren<SpriteRenderer>();
        if (!sr) return Vector2.one;
        defaultSprite = sr.sprite;
        Vector2 scale = p.transform.localScale;
        //s = new Vector2(sr.bounds.size.x / scale.x, sr.bounds.size.y / scale.y);
        s = new Vector2(sr.sprite.rect.size.x / scale.x, sr.sprite.rect.size.y / scale.y);
        pivot = defaultSprite.pivot;
        pivot.x /= s.x;
        pivot.y /= s.y;
        pivotOffset = new Vector2(.5f, .5f) - pivot;
        pivotOffset *= 2;
        s /= sr.sprite.pixelsPerUnit;
        
        DestroyImmediate(p);
        return s;
    }
    

    void Update()
    {
        autoRebuild();

		if (exceptions != null)
		for (int i = 0; i < exceptions.Length; i++) 
		{
			exceptions[i].x = (int)exceptions[i].x;
			exceptions[i].y = (int)exceptions[i].y;
		}
    }

    public void rebuild(bool forceBuild=false)
    {
        if (!tr) Awake();
        //if (Application.isPlaying) return;
        if (recentScale == transform.localScale && recentClones.isSameAs(clones) && !forceBuild) return;
        recentScale = transform.localScale;
        recentClones.set(clones);

        /*destroy all children*/
        int chidren = tr.childCount; 
        for (int i = 0; i < chidren; i++)
            DestroyImmediate(tr.GetChild(0).gameObject);
        //box = new GameObject("Box:"+name).transform;
        //box.SetParent(tr);
        //box.localPosition = Vector3.zero;
        //box.localScale = Vector3.one; 

        /*Loop through and spit out repeated tiles*/
        GameObject child;
        if (clones!=null)
            for (int y = -clones.down; y <= clones.up; y++)
            {
                parent = new GameObject("Y=" + y + ": " + name).transform;
                parent.SetParent(tr);
                parent.localScale = Vector3.one;
                parent.localPosition = Vector3.zero;
                parent.localRotation = new Quaternion();
                for (int x = -clones.left; x <= clones.right; x++)
                {
                    child = create
                        (
                            (new Vector3(size.x, 0, 0) * x) +
                            (new Vector3(0, size.y, 0) * y)
                        );
                    child.name = "X=" + x + ": " + name;

                    /*Flipping*/
                    Vector3 scale = child.transform.localScale,
                            pos = child.transform.localPosition;
                    if (flippingX && Mathf.Abs(x) % 2 == 0)
                    {
                        scale.x *= -1;
                        pos.x += pivotOffset.x * size.x;
                    }
                    if (flippingY && Mathf.Abs(y) % 2 == 0)
                    {
                        scale.y *= -1;
                        pos.y += pivotOffset.y * size.y;
                    }
                    child.transform.localScale = scale;
                    child.transform.localPosition = pos;

                    /*Exceptions*/
                    if (exceptions != null)
                        for (int i = 0; i < exceptions.Length; i++)
                        {
                            if (exceptions[i].x == x && exceptions[i].y == y)
                            {
                                child.SetActive(false);
                                break;
                            }
                        }
                }
            }

        if (drawBlank) createBlank();

        setHideFlags(true);
    }

    public void autoRebuild()
    {
        if (autoRefresh) rebuild();
    }

    GameObject create(Vector2 offset)
    {
        GameObject g = Instantiate(prefab) as GameObject;
        Transform t = g.transform;
        t.SetParent(parent);
        t.localPosition = offset;
        t.localScale = Vector3.one;
        t.localRotation = new Quaternion();
        DestroyImmediate(g.GetComponent<RepeatedGameObject>());
        return g;
    }

    void createBlank()
    {
        if (!blank) return;
        GameObject g = Instantiate(blank) as GameObject;
        g.name = "BLANK-" + name;
        Transform t = g.transform;
        t.SetParent(tr);
        t.localPosition = new Vector3(
            (clones.right-clones.left) * size.x / 2, 
            -size.y * (clones.down),// - sizeBlank.y / 2, 
            0);
        t.localScale = new Vector3(
            size.x * (float)(clones.right + clones.left + 1) / sizeBlank.x,
            blankLenght / sizeBlank.y, 1);
        t.localRotation = new Quaternion();
    }

    public void setHideFlags(bool hidden)
    {
        int children = transform.childCount;
        for (int i = 0; i < children; i++)
        {
            transform.GetChild(i).hideFlags = hidden ? HideFlags.HideInHierarchy | HideFlags.HideInInspector : HideFlags.None;
        }
    }

    public bool getHideFlags()
    {
        return transform.childCount <= 0 || transform.GetChild(0).hideFlags != HideFlags.None;
    }
}
