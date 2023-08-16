using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Xunit;

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
                    AddEvent(InitializationTests.ChildAddedEvent + ":" + ((InitializationTestsClass) ni).Id);
                }
            };
        }
        
        public string Property
        {
            get => _prop;
            set
            {
                _prop = value;
                AddEvent(InitializationTests.PropertySetEvent);
            }
        }
        
        public InitializationTestsClass Child
        {
            get => _child;
            set
            {
                _child = value;
                AddEvent(InitializationTests.ChildAddedEvent + ":" + value.Id);
            }
        }
        
    }
    
    

    public class InitializationTestsSupportInitializeClass : InitializationTestsClass, ISupportInitialize
    {
        public void BeginInit()
        {
            AddEvent(InitializationTests.BeginInitEvent);
        }

        public void EndInit()
        {
            AddEvent(InitializationTests.EndInitEvent);
        }
    }

    [UsableDuringInitialization(true)]
    public class InitializationTestsTopDownClass : InitializationTestsSupportInitializeClass
    {
        
    }
    
    
    public class InitializationTests : CompilerTestBase
    {
        public static string BeginInitEvent = "BeginInit";
        public static string EndInitEvent = "EndInit";
        public static string PropertySetEvent = "PropertySet";
        public static string ChildAddedEvent = "ChildAdded";

        public InitializationTests()
        {
            InitializationTestsClass.Reset();
        }
        
        [Fact]
        public void Initialization_Events_Should_Be_Triggered_For_Supports_Initialize()
        {
            
            CompileAndRun(@"
<InitializationTestsSupportInitializeClass xmlns='test' Property='123'>
    <InitializationTestsSupportInitializeClass.Child>
        <InitializationTestsSupportInitializeClass Property='321'/>
    </InitializationTestsSupportInitializeClass.Child>
    <InitializationTestsSupportInitializeClass Property='321'/>
</InitializationTestsSupportInitializeClass>");
            Helpers.StructDiff(new List<string>
            {
                "1:BeginInit",
                "1:PropertySet",
                "2:BeginInit",
                "2:PropertySet",
                "2:EndInit",
                "1:ChildAdded:2",
                "3:BeginInit",
                "3:PropertySet",
                "3:EndInit",
                "1:ChildAdded:3",
                "1:EndInit"
            }, InitializationTestsClass.Events);
        }
        
        [Fact]
        public void UsableDuringInitialization_Should_Revert_Initialization_Order()
        {
            CompileAndRun(@"
<InitializationTestsTopDownClass xmlns='test' Property='123'>
    <InitializationTestsTopDownClass.Child>
        <InitializationTestsTopDownClass Property='321'/>
    </InitializationTestsTopDownClass.Child>
    <InitializationTestsTopDownClass Property='321'/>
</InitializationTestsTopDownClass>");
            Helpers.StructDiff(new List<string>
            {
                "1:BeginInit",
                "1:PropertySet",
                "2:BeginInit",
                "1:ChildAdded:2",
                "2:PropertySet",
                "2:EndInit",
                "3:BeginInit",
                "1:ChildAdded:3",
                "3:PropertySet",
                "3:EndInit",
                "1:EndInit"
            }, InitializationTestsClass.Events);
        }
        
        
        
    }
}