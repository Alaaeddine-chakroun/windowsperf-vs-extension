﻿// BSD 3-Clause License
//
// Copyright (c) 2022, Arm Limited
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
//
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using WindowsPerfGUI.Components.TreeListView;
using WindowsPerfGUI.SDK.WperfOutputs;
using WindowsPerfGUI.Utils;

namespace WindowsPerfGUI.ToolWindows.SamplingExplorer
{
    public class FormattedSamplingResults : NotifyPropertyChangedImplementor, ITreeModel
    {

        private List<SamplingSection> rootSampledEvent;
        public List<SamplingSection> RootSampledEvent
        {
            get { return rootSampledEvent; }
            set
            {
                rootSampledEvent = value;
                OnPropertyChanged();
            }
        }

        public FormattedSamplingResults()
        {
            RootSampledEvent = new List<SamplingSection>();
        }
        public void ClearSampling()
        {
            RootSampledEvent = new List<SamplingSection>();
        }
        public void FormatSamplingResults(WperfSampling wperSamplingOutput, string rootName = "Root")
        {
            var rootSample = new SamplingSection()
            {
                PdbFile = wperSamplingOutput.SamplingSummary.PdbFile,
                PeFile = wperSamplingOutput.SamplingSummary.PeFile,
                SamplesDropped = wperSamplingOutput.SamplingSummary.SamplesDropped,
                Modules = wperSamplingOutput.SamplingSummary.Modules,
                Hits = wperSamplingOutput.SamplingSummary.SamplesGenerated,
                Overhead = 100,
                Name = rootName,
                Layer = 0,
                Children = new List<SamplingSection>(),
                Parent = null,
                SectionType = SamplingSection.SamplingSectionType.ROOT,
            };
            RootSampledEvent.Add(rootSample);
            foreach (SampledEvent sampledEvent in wperSamplingOutput.SamplingSummary.SampledEvents)
            {
                SamplingSection eventSection = new SamplingSection()
                {
                    Name = sampledEvent.Type,
                    Children = new List<SamplingSection>(),
                    Hits = sampledEvent.SampleList.Count,
                    Layer = 1,
                    SectionType = SamplingSection.SamplingSectionType.SAMPLE_EVENT,
                    Frequency = sampledEvent.Interval.ToString(),
                    Parent = rootSample,
                };

                rootSample.Children.Add(eventSection);
                foreach (SampleResult sample in sampledEvent.SampleList)
                {
                    SamplingSection samplesSection = new SamplingSection()
                    {
                        Children = new List<SamplingSection>(),
                        Name = sample.Symbol,
                        Hits = sample.Count,
                        Overhead = sample.Overhead,
                        Layer = 2,
                        SectionType = SamplingSection.SamplingSectionType.SAMPLE_FUNCTION,
                        Parent = eventSection,
                    };
                    eventSection.Children.Add(samplesSection);
                    Annotate annotatedSample = sampledEvent.Annotate.Find((x) => x.FunctionName == sample.Symbol);
                    if (annotatedSample == null) continue;
                    foreach (SourceCode annotationSourceCode in annotatedSample.SourceCode)
                    {
                        SamplingSection annotationSection = new SamplingSection()
                        {
                            Name = annotationSourceCode.Filename,
                            Hits = annotationSourceCode.Hits,
                            Overhead = CalculateFunctionPercentage(
                                          annotationSourceCode.Hits,
                                          (double)samplesSection.Hits
                                          ),
                            Layer = 3,
                            SectionType = SamplingSection.SamplingSectionType.SAMPLE_SOURCE_CODE,
                            Parent = samplesSection,
                            LineNumber = annotationSourceCode.LineNumber,
                            IsFileExists = File.Exists(annotationSourceCode.Filename)
                        };
                        samplesSection.Children.Add(annotationSection);
                    }
                }

            }

            OnPropertyChanged("RootSampledEvent");
        }


        private double CalculateFunctionPercentage(double value, double total)
        {
            return Math.Min(value / total * 100, 100);
        }

        public IEnumerable GetChildren(object parent)
        {
            return parent == null ? RootSampledEvent : (IEnumerable)((SamplingSection)parent).Children;
        }

        public bool HasChildren(object parent)
        {
            return ((SamplingSection)parent).Children?.Count > 0;
        }
    }
}