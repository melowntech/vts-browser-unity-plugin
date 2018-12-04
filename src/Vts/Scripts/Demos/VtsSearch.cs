/**
 * Copyright (c) 2017 Melown Technologies SE
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * *  Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 *
 * *  Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using vts;

// these structs emit warnings that some fields are not being used, however they are used in the serialization
#pragma warning disable

[Serializable]
internal struct Position
{
    public double x, y, z;
}

[Serializable]
internal class Item
{
    public string displayName;
    public Position position;
    public double radius;
    // there is actually more information in the json, but not needed in this example
}

#pragma warning restore

public class VtsSearch : MonoBehaviour
{
    public InputField input;
    public Dropdown dropDown;

    private Map map;
    private vts.Navigation nav;
    private SearchTask task;
    private List<Item> results = new List<Item>();

    public void Typing()
    {
        dropDown.interactable = false;
        string s = input.text;
        task = map.Search(s); // initiate search task
    }

    public void Selected(int index)
    {
        Item it = results[index];
        double[] p = new double[3];
        p[1] = 270;
        nav.SetRotation(p); // nadir view
        p[0] = it.position.x;
        p[1] = it.position.y;
        p[2] = it.position.z;
        nav.SetPoint(p); // location of the result
        nav.SetViewExtent(it.radius > 3000 ? it.radius * 2 : 6000); // some reasonable view extent (zoom)
        nav.SetOptions("{\"navigationType\":2}"); // switch to fly-over navigation mode
    }

    void Start()
    {
        map = GetComponent<VtsMap>().GetVtsMap();
        input.interactable = false;
        dropDown.interactable = false;
    }

    void Update()
    {
        if (nav == null)
        {
            nav = FindObjectOfType<UnityEngine.Camera>().GetComponent<VtsNavigation>().GetVtsNavigation();
            return;
        }
        if (map.GetMapconfigAvailable() && map.GetSearchable())
        {
            input.interactable = true;
            // the check method will update the results list if the data are already available
            if (task != null && task.Check())
            {
                dropDown.options.Clear();
                results.Clear();
                foreach (string sr in task.results)
                {
                    Item it = JsonUtility.FromJson<Item>(sr);
                    results.Add(it);
                    Dropdown.OptionData o = new Dropdown.OptionData(it.displayName);
                    dropDown.options.Add(o);
                }
                dropDown.value = -1;
                dropDown.interactable = true;
                task = null;
            }
        }
    }
}
