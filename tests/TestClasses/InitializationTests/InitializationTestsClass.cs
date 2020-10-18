using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XamlParserTests
{
    public class InitializationTestsClass
    {
        private string _prop;

        private InitializationTestsClass _child;

        [ThreadStatic]
        public static List<string> Events;

        [ThreadStatic]
        public static int NextId;

        public static void Reset()
        {
            Events = new List<string>();
            NextId = 0;
        }

        public readonly int Id;

        [Content]
        public ObservableCollection<InitializationTestsClass> Children { get; } = new ObservableCollection<InitializationTestsClass>();

        public void AddEvent(string ev)
        {
            Events.Add($"{Id}:{ev}");
        }

        public InitializationTestsClass()
        {
            Id = ++NextId;
            Children.CollectionChanged += (_, ev) =>
            {
                foreach (var ni in ev.NewItems)
                {
                    AddEvent(EventConstants.ChildAddedEvent + ":" + ((InitializationTestsClass)ni).Id);
                }
            };
        }

        public string Property
        {
            get => _prop;
            set
            {
                _prop = value;
                AddEvent(EventConstants.PropertySetEvent);
            }
        }

        public InitializationTestsClass Child
        {
            get => _child;
            set
            {
                _child = value;
                AddEvent(EventConstants.ChildAddedEvent + ":" + value.Id);
            }
        }
    }
}
