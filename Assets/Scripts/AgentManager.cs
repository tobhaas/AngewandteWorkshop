﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BehaviorDesigner.Runtime;
using System.Linq;

public class AgentManager : MonoBehaviour
{
    public static AgentManager Instance { get; private set; } //for singleton

    [Header("GameObjects")]
    public string TargetTag = "Target";

    public string EntranceTag = "Entrance";
    public GameObject[] AgentPrefab;

    private GameObject[] _entrances;
    private Dictionary<GameObject, GameObject> _targets = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, GameObject> _agents = new Dictionary<GameObject, GameObject>();

    public float EntranceRate = 1.25f; //the rate at which agents will enter
    private float _lastEntrance = 0; //time since last entrance

    [Header("UI")]
    public Text AgentCount;

    public Text CarrierCount;
    public Slider InfectedSlider;
    public Text Percent;

    private void UpdateUI()
    {
        //setup counts
        int agentCount = _agents.Count;
        int carrierCount = 0;
        int infectedCount = 0;

        //loop through all agents to find health status info
        GameObject[] agents = _agents.Keys.ToArray();
        foreach (GameObject agent in agents)
        {
            AgentHealth health = agent.GetComponent<AgentHealth>();
            if (health == null) continue;

            if (health.Status == AgentHealth.HealthStatus.CARRIER) { carrierCount++; }
            if (health.Status == AgentHealth.HealthStatus.INFECTED) { infectedCount++; }
        }
        float percent = infectedCount / (agentCount * 1.00f);

        //set UI text and slider
        if (AgentCount != null) { AgentCount.text = "Agents: " + agentCount; }
        if (CarrierCount != null) { CarrierCount.text = "Carriers: " + carrierCount; }
        if (InfectedSlider != null) { InfectedSlider.value = percent; }
        if (Percent != null) { Percent.text = percent * 100 + "%"; }
    }

    private void Awake()
    {
        //Singleton Pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        _entrances = GameObject.FindGameObjectsWithTag(EntranceTag);
        GameObject[] targets = GameObject.FindGameObjectsWithTag(TargetTag); //grabbing all targets in scene
        foreach (GameObject target in targets) //loop through each target
        {
            if (_targets.ContainsKey(target)) continue; //if target already in dictionary skip
            _targets.Add(target, null);
        }
        InstantiateAgent(AgentPrefab[Random.Range(0, AgentPrefab.Length)]);
    }

    private void Update()
    {
        if (EntranceRate <= _lastEntrance)
        {
            _lastEntrance = 0;
            InstantiateAgent(AgentPrefab[Random.Range(0, AgentPrefab.Length)]);
        }
        else //if(EntranceRate > _lastEntrance)
        {
            _lastEntrance += Time.deltaTime;
        }

        //update the UI at end of Update
        UpdateUI();
    }

    private void InstantiateAgent(GameObject prefab)
    {
        if (_agents.Count >= _targets.Count - 1) return;

        GameObject entrance = _entrances[Random.Range(0, _entrances.Length)];

        Vector3 position = entrance.transform.position;
        GameObject agent = Instantiate(prefab, position, Quaternion.identity);
        BehaviorTree behaviorTree = agent.GetComponent<BehaviorTree>();
        behaviorTree.SetVariableValue("Entrance", entrance); //assumes public tree variable Entrance
        behaviorTree.EnableBehavior();
        _agents.Add(agent, null);
    }

    public void RemoveAgent(GameObject agent)
    {
        GameObject lastTarget = _agents[agent];
        _targets[lastTarget] = null; //remove agent from target
        _agents.Remove(agent); //remove agent from agents dictionary
        Destroy(agent);
    }

    public GameObject GetTarget(GameObject agent)
    {
        //make sure the agent doesn't go to previous target
        GameObject lastTarget = _agents[agent];
        if (lastTarget != null)
        {
            _targets[lastTarget] = null; //this target is open for a new agent
        }

        GameObject[] keys = _targets.Keys.ToArray();
        keys = Shuffle(keys);
        for (int i = 0; i < keys.Length; i++)
        {
            GameObject key = keys[i];

            if (lastTarget == key) continue; //if target was previous target, skip
            if (_targets[key] != null) continue; //if target has agent assigned, skip

            _targets[key] = agent;
            _agents[agent] = key;
            return key; //key is a target
        }

        return null;
    }

    private GameObject[] Shuffle(GameObject[] objects)
    {
        GameObject tempGO;
        for (int i = 0; i < objects.Length; i++)
        {
            //Debug.Log("i: " + i);
            int rnd = Random.Range(0, objects.Length);
            tempGO = objects[rnd];
            objects[rnd] = objects[i];
            objects[i] = tempGO;
        }
        return objects;
    }
}