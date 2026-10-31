using System;
using NightTrolleybus.Items;
using UnityEngine;

namespace NightTrolleybus.Narrative
{
    /// <summary>Один варіант вибору в події та його наслідки.</summary>
    [Serializable]
    public class RoadEventChoice
    {
        public string text;
        [TextArea] public string outcome;
        [Tooltip("Предмет, без якого цей вибір недоступний. Його забирають")]
        public ItemData requiredItem;
        public ItemData rewardItem;
        public float flameChange;
        public float temperatureChange;
        public float powerChange;
    }

    /// <summary>Текстова подія на дорозі з вибором і наслідками.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Road Event", fileName = "RoadEvent")]
    public class RoadEventData : ScriptableObject
    {
        [SerializeField] private string title;
        [TextArea(3, 8)]
        [SerializeField] private string description;
        [SerializeField] private RoadEventChoice[] choices;

        public string Title => title;
        public string Description => description;
        public RoadEventChoice[] Choices => choices;
    }
}
