﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bddify.Core;

namespace Bddify.Scanners
{
    public class ScanForScenarios : IScanForScenarios
    {
        private readonly IEnumerable<IScanForSteps> _stepScanners;
        private readonly string _scenarioTextTemplate;

        public ScanForScenarios(IEnumerable<IScanForSteps> stepScanners, string scenarioTextTemplate = null)
        {
            _stepScanners = stepScanners;
            _scenarioTextTemplate = scenarioTextTemplate;
        }

        public IEnumerable<Scenario> Scan(Type scenarioType)
        {
            var argsSet = GetArgsSets(scenarioType);
            if (argsSet.Any())
                return argsSet.Select(a => GetScenario(scenarioType, argsSet: a));

            return new[] { GetScenario(scenarioType) };
        }

        protected virtual IEnumerable<object[]> GetArgsSets(Type scenarioType)
        {
            var runWithScenarioAtts = (RunScenarioWithArgsAttribute[])scenarioType.GetCustomAttributes(typeof(RunScenarioWithArgsAttribute), false);

            return runWithScenarioAtts.Select(argSet => argSet.ScenarioArguments).ToList();
        }

        static string GetScenarioText(Type scenarioType)
        {
            return NetToString.Convert(scenarioType.Name);
        }

        protected virtual Scenario GetScenario(Type scenarioType, object[] argsSet = null)
        {
            var scenarioText = _scenarioTextTemplate ?? GetScenarioText(scenarioType);
            if (argsSet != null)
            {
                if (string.IsNullOrEmpty(_scenarioTextTemplate))
                    scenarioText += " " + string.Join(", ", argsSet);
                else
                    scenarioText = string.Format(_scenarioTextTemplate, argsSet);
            }

            object testObject = Activator.CreateInstance(scenarioType);

            var steps = ScanScenarioForSteps(scenarioType); 

            return new Scenario(testObject, steps, scenarioText, argsSet);
        }

        private IEnumerable<ExecutionStep> ScanScenarioForSteps(Type scenarioType)
        {
            var steps = new List<ExecutionStep>();
            // scanners are sorted by priority to make sure the higher priority scanners get to scan the scenario first
            foreach (var scanner in _stepScanners.OrderBy(s => s.Priority))
            {
                foreach (var step in scanner.Scan(scenarioType))
                {
                    // if a method has been found by another scanner, ignore it
                    if (steps.Any(s => s.Equals(step))) 
                        continue;

                    steps.Add(step);
                    yield return step;
                }
            }
        }
    }
}