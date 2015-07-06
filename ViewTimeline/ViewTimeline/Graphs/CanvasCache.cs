//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading;

//namespace ViewTimeline.Graphs
//{
//    /// <summary>
//    /// A single cache element, containing a fully derived graph over the recorded interval
//    /// </summary>
//    internal class CacheElement : IComparable<CacheElement>
//    {
//        private CanvasData _data;
//        private DateTime _startTime;
//        private DateTime _endTime;

//        public CacheElement(CanvasData data, DateTime startTime, DateTime endTime)
//        {
//            if (data == null) throw new ArgumentNullException("CaptureGraph in cache cannot be null");
//            this._data = data;
//            this._startTime = startTime;
//            this._endTime = endTime;
//        }

//        private CacheElement(DateTime startTime, DateTime endTime)
//        {
//            this._data = null;
//            this._startTime = startTime;
//            this._endTime = endTime;
//        }

//        public CanvasData Graph
//        {
//            get { return _data; }
//        }

//        public static CacheElement EmptyElement(DateTime startTime, DateTime endTime)
//        {
//            return new CacheElement(startTime, endTime);
//        }

//        public int CompareTo(CacheElement other)
//        {
//            //if other is contained in current element, current element may be substituted (since it already exists
//            if (_startTime <= other._startTime && _endTime >= other._endTime) return 0;
//            if (_startTime >= other._startTime && _endTime <= other._endTime)
//            {
//                //copy other data into current element
//                Semaphore sem = new Semaphore(0, 0); //ensure threads don't mangle cache
//                _startTime = other._startTime;
//                _endTime = other._endTime;
//                _data = other._data;
//                sem.Release();
//                return 0;
//            }
//            if (_startTime < other._startTime) return -1;
//            if (_startTime > other._startTime) return 1;
//            return 0;
//        }
//    }

//    /// <summary>
//    /// Storage for a set of cache elements, for a particular root and render depth
//    /// </summary>
//    internal class CacheLine : IComparable<CacheLine>
//    {
//        private List<CacheElement> _line;
//        private FrameNodeLevel _renderDepth;

//        public FrameNodeLevel RenderDepth { get { return _renderDepth; } }

//        public CacheLine(FrameNodeLevel renderDepth)
//        {
//            _renderDepth = renderDepth;
//            _line = new List<CacheElement>();
//        }

//        public void AddElement(CanvasData data, DateTime startTime, DateTime endTime)
//        {
//            var elem = new CacheElement(data, startTime, endTime);

//            Semaphore sem = new Semaphore(0, 1); //prevent adding element after another thread has already executed the if condition
//            if (_line.Contains(elem)) return;
//            _line.Add(elem);
//            sem.Release();
//        }

//        public CanvasData SearchCacheLine(DateTime startTime, DateTime endTime)
//        {
//            var searchKey = CacheElement.EmptyElement(startTime, endTime);
//            if (_line.Contains(searchKey))
//            {
//                return _line.Find(element => element == searchKey).Graph;
//            }
//            return null;
//        }

//        public int CompareTo(CacheLine other)
//        {
//            if ((int)_renderDepth < (int)other._renderDepth) return -1;
//            if ((int)_renderDepth < (int)other._renderDepth) return 1;
//            return 0;
//        }
//    }

//    /// <summary>
//    /// Storage for a set of cache lines, at a particular root depth
//    /// </summary>
//    internal class CachePage
//    {
//        private List<CacheLine> _page;

//        public CachePage()
//        {
//            _page = new List<CacheLine>();
//        }

//        public void AddElement(CanvasData data, FrameNodeLevel renderDepth, DateTime startTime, DateTime endTime)
//        {
//            //if element fits in existing cache line, just add
//            foreach (CacheLine line in _page)
//            {
//                if (line.RenderDepth == renderDepth)
//                {
//                    line.AddElement(data, startTime, endTime);
//                    return;
//                }
//            }
//            //otherwise create new cache line with element in it
//            var tmp = new CacheLine(renderDepth);
//            tmp.AddElement(data, startTime, endTime);
//            _page.Add(tmp);
//            _page.Sort();
//        }

//        public CanvasData SearchCachePage(FrameNodeLevel renderDepth, DateTime startTime, DateTime endTime)
//        {
//            foreach (CacheLine line in _page)
//            {
//                if (line.RenderDepth == renderDepth)
//                {
//                    var graph = line.SearchCacheLine(startTime, endTime);
//                    if (graph != null) return graph;
//                }
//            }
//            return null;
//        }
//    }

//    public class PreCacheDirectives
//    {
//        private FrameElement _cacheRoot;
//        private FrameNodeLevel _renderUnit;
//        private DateTime _start;
//        private DateTime _end;

//        public PreCacheDirectives(FrameNodeLevel renderUnit, DateTime start, DateTime end, FrameElement cacheRoot)
//        {
//            _renderUnit = renderUnit;
//            _start = start;
//            _end = end;
//            _cacheRoot = cacheRoot;
//        }

//        public FrameElement CacheRoot
//        {
//            get { return _cacheRoot; }
//        }

//        public DateTime End
//        {
//            get { return _end; }
//        }

//        public DateTime Start
//        {
//            get { return _start; }
//        }

//        public FrameNodeLevel RenderUnit
//        {
//            get { return _renderUnit; }
//        }
//    }

//    public class CacheManager
//    {
//        private readonly FileManager _manager;
//        private readonly FrameNodeLevel _startRenderUnit;
//        private readonly DateTime _startTime;
//        private readonly DateTime _endTime;
//        private CachePage _page;
//        private Mutex _mutex;

//        public CanvasData CurrentGraph { get; private set; }

//        public CacheManager(FileManager manager, FrameNodeLevel startRenderUnit, DateTime startTime, DateTime endTime)
//        {
//            _manager = manager;
//            _startRenderUnit = startRenderUnit;
//            _startTime = startTime;
//            _endTime = endTime;
//            _page = new CachePage();
//            _mutex = new Mutex();

//            //create initial graph
//            CurrentGraph = CanvasManager.GenerateCanvasData(startTime, endTime, startRenderUnit);
//            _page.AddElement(CurrentGraph, startRenderUnit, startTime, endTime);

//            BeginPreCache(new PreCacheDirectives(startRenderUnit, startTime, endTime, manager.CurrentElement));
//        }

//        public void SetCurrentGraph(FrameNodeLevel renderDepth, DateTime startTime, DateTime endTime)
//        {
//            _mutex.WaitOne();
//            var graph = _page.SearchCachePage(renderDepth, startTime, endTime);
//            _mutex.ReleaseMutex();

//            if (graph == null) //graph doesn't exist
//            {
//                graph = CreateCaptureGraph(_manager.CurrentElement, renderDepth, startTime, endTime);
//                _page.AddElement(graph, renderDepth, startTime, endTime);
//            }
//            CurrentGraph = graph;

//            //pre-cache around new element in asynchronous thread
//            BeginPreCache(new PreCacheDirectives(renderDepth, startTime, endTime, _manager.CurrentElement));
//        }

//        private void BeginPreCache(PreCacheDirectives directives)
//        {
//            Thread preCache = new Thread(PreCache) { IsBackground = true };
//            preCache.Start(directives);
//        }

//        private void PreCache(object data)
//        {
//            PreCacheDirectives directives = data as PreCacheDirectives;

//            //cache next level of graph for entire time span
//            Debug.Assert(directives != null, "directives != null");
//            if (directives.RenderUnit != FrameNodeLevel.Second)
//            {
//                var cacheUnit = (FrameNodeLevel)(((int)directives.RenderUnit) + 1);
//                AddPreCacheEntry(directives.CacheRoot, cacheUnit, directives.Start, directives.End);
//            }
//            //cache elements to the side

//            switch (directives.RenderUnit)
//            {
//                case FrameNodeLevel.Month:
//                case FrameNodeLevel.Day: //should already be cached, so unlikely to actually run
//                    if (directives.Start > _manager.StartTime || directives.End < _manager.EndTime)
//                    {
//                        AddPreCacheEntry(directives.CacheRoot, directives.RenderUnit, _manager.StartTime, _manager.EndTime);
//                    }
//                    break;
//                case FrameNodeLevel.Hour:
//                    if (directives.Start > _manager.StartTime)
//                    {
//                        var start = directives.Start.AddMonths(-1);
//                        if (start < _manager.StartTime) start = _manager.StartTime;

//                        AddPreCacheEntry(directives.CacheRoot, FrameNodeLevel.Hour, start, directives.Start);
//                    }
//                    if (directives.End < _manager.EndTime)
//                    {
//                        var end = directives.End.AddMonths(1);
//                        if (end > _manager.EndTime) end = _manager.EndTime;

//                        AddPreCacheEntry(directives.CacheRoot, FrameNodeLevel.Hour, directives.End, end);
//                    }
//                    break;
//                case FrameNodeLevel.Minute:
//                    if (directives.Start > _manager.StartTime)
//                    {
//                        var start = directives.Start.AddDays(-1);
//                        if (start < _manager.StartTime) start = _manager.StartTime;

//                        AddPreCacheEntry(directives.CacheRoot, FrameNodeLevel.Minute, start, directives.Start);
//                    }
//                    if (directives.End < _manager.EndTime)
//                    {
//                        var end = directives.End.AddDays(1);
//                        if (end > _manager.EndTime) end = _manager.EndTime;

//                        AddPreCacheEntry(directives.CacheRoot, FrameNodeLevel.Minute, directives.End, end);
//                    }
//                    break;
//                case FrameNodeLevel.Second:
//                    if (directives.Start > _manager.StartTime)
//                    {
//                        var start = directives.Start.AddHours(-1);
//                        if (start < _manager.StartTime) start = _manager.StartTime;

//                        AddPreCacheEntry(directives.CacheRoot, FrameNodeLevel.Second, start, directives.Start);
//                    }
//                    if (directives.End < _manager.EndTime)
//                    {
//                        var end = directives.End.AddHours(1);
//                        if (end > _manager.EndTime) end = _manager.EndTime;

//                        AddPreCacheEntry(directives.CacheRoot, FrameNodeLevel.Second, directives.End, end);
//                    }
//                    break;
//                default:
//                    throw new ArgumentOutOfRangeException();
//            }
//        }

//        private void AddPreCacheEntry(FrameElement cacheRoot, FrameNodeLevel renderUnit, DateTime start, DateTime end)
//        {
//            _mutex.WaitOne(); //do not allow multi-part modifications to cache during graph add or fetch
//            if (_page.SearchCachePage(renderUnit, start, end) == null) //ensure element doesnt already exist
//            {
//                var graph = CreateCaptureGraph(cacheRoot, renderUnit, start, end);
//                _page.AddElement(graph, renderUnit, start, end);
//            }
//            _mutex.ReleaseMutex();
//        }

//        private CaptureGraph CreateCaptureGraph(FrameElement graphRoot, FrameNodeLevel renderUnit, DateTime start, DateTime end)
//        {
//            var graph = new CaptureGraph(_manager);
//            graph.SetViewport(_manager.ViewportSize);
//            graphRoot.GenerateGraphStructure(start, end, renderUnit, ref graph);
//            graph.Initialise();
//            return graph;
//        }
//    }
//}