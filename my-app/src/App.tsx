import React, { useState, useRef, useEffect } from "react";

// Constants
const FRAME_RATE = 30; // frames per second
const PIXELS_PER_SECOND = 50; // base zoom scale
const MIN_ZOOM = 0.5;
const MAX_ZOOM = 4;

const defaultClips = [
  { id: "clip1", start: 0, duration: 3, name: "Intro" },
  { id: "clip2", start: 3, duration: 5, name: "Content" },
  { id: "clip3", start: 8, duration: 2, name: "Outro" },
];

function formatTime(seconds) {
  return `${seconds.toFixed(2)}s`;
}

function App() {
  const [clips, setClips] = useState(defaultClips);
  const [currentTime, setCurrentTime] = useState(0);
  const [playing, setPlaying] = useState(false);
  const [zoom, setZoom] = useState(1);
  const [scrollLeft, setScrollLeft] = useState(0);
  const [tool, setTool] = useState("select"); // or 'cut'
  const [draggingPlayhead, setDraggingPlayhead] = useState(false);

  const timelineRef = useRef(null);
  const rafRef = useRef(null);

  // Calculate timeline width dynamically based on clips
  const calculateTimelineWidth = () => {
    const maxEnd = clips.reduce(
      (max, c) =>
        Math.max(max, (c.start + c.duration) * PIXELS_PER_SECOND * zoom),
      0,
    );
    return maxEnd + 100; // padding
  };
  const timelineWidth = calculateTimelineWidth();

  // Play/pause animation loop
  useEffect(() => {
    if (playing) {
      const startTime = performance.now() - currentTime * 1000;
      const loop = (time) => {
        const newTime = (time - startTime) / 1000;
        if (newTime >= getTotalDuration()) {
          setPlaying(false);
          setCurrentTime(getTotalDuration());
          return;
        }
        setCurrentTime(newTime);
        rafRef.current = requestAnimationFrame(loop);
      };
      rafRef.current = requestAnimationFrame(loop);
      return () => cancelAnimationFrame(rafRef.current);
    }
  }, [playing]);

  // Total duration of timeline clips end
  const getTotalDuration = () => {
    return clips.reduce((end, c) => Math.max(end, c.start + c.duration), 0);
  };

  // Handle ctrl + wheel for zoom, wheel for horizontal scroll
  const onWheel = (e) => {
    e.preventDefault();
    if (e.ctrlKey) {
      // adjust zoom
      let newZoom = zoom - e.deltaY * 0.01;
      newZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, newZoom));
      setZoom(newZoom);
      setScrollLeft(0); // reset scroll on zoom
    } else {
      // scroll timeline
      let newScroll = scrollLeft + e.deltaY;
      newScroll = Math.max(
        0,
        Math.min(
          timelineWidth - (timelineRef.current?.clientWidth || 0),
          newScroll,
        ),
      );
      setScrollLeft(newScroll);
    }
  };

  // Convert position to time, snap to frame
  const posToTime = (pos) => {
    const t = (pos + scrollLeft) / (PIXELS_PER_SECOND * zoom);
    return Math.round(t * FRAME_RATE) / FRAME_RATE;
  };

  // Convert time to position
  const timeToPos = (time) => {
    return time * PIXELS_PER_SECOND * zoom - scrollLeft;
  };

  // Find clip under a time
  const getClipAtTime = (time) =>
    clips.find(
      (clip) => time >= clip.start && time <= clip.start + clip.duration,
    );

  // Drag playhead handlers for time bar dragging
  const onPlayheadMouseDown = (e) => {
    e.preventDefault();
    setDraggingPlayhead(true);
  };
  const onMouseMove = (e) => {
    if (draggingPlayhead && timelineRef.current) {
      const rect = timelineRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      let t = posToTime(x);
      t = Math.min(getTotalDuration(), Math.max(0, t));
      setCurrentTime(t);
    }
  };
  const onMouseUp = () => {
    if (draggingPlayhead) setDraggingPlayhead(false);
  };

  // Move clip with select tool
  const onClipDrag = (id, dx) => {
    if (tool !== "select") return;
    setClips((prev) => {
      const clip = prev.find((c) => c.id === id);
      if (!clip) return prev;
      // calculate proposed new start time
      let newStart = clip.start + dx / (PIXELS_PER_SECOND * zoom);
      if (newStart < 0) newStart = 0;

      // prevent overlaps
      const otherClips = prev.filter((c) => c.id !== id);
      for (let oc of otherClips) {
        if (
          (newStart >= oc.start && newStart < oc.start + oc.duration) || // start inside other clip
          (newStart + clip.duration > oc.start &&
            newStart + clip.duration <= oc.start + oc.duration) || // end inside other clip
          (newStart <= oc.start &&
            newStart + clip.duration >= oc.start + oc.duration) // fully contains other clip
        ) {
          if (newStart < oc.start) newStart = oc.start - clip.duration;
          else newStart = oc.start + oc.duration;
        }
      }
      newStart = Math.max(0, newStart);
      // Round to nearest frame
      newStart = Math.round(newStart * FRAME_RATE) / FRAME_RATE;
      const updated = prev.map((c) =>
        c.id === id ? { ...c, start: newStart } : c,
      );
      return updated;
    });
  };

  // On drag end, update scrollable width so new clips visible
  const onClipDragEnd = () => {
    // Update scrollLeft limit to allow seeing new clips
    // If current scrollLeft > max allowed, adjust
    const maxScroll = timelineWidth - (timelineRef.current?.clientWidth || 0);
    if (scrollLeft > maxScroll) {
      setScrollLeft(Math.max(0, maxScroll));
    }
  };

  // Cut tool: Split clip at currentTime snapped to nearest frame
  const onCutAt = (posX) => {
    if (tool !== "cut") return;
    const time = posToTime(posX);
    const clip = getClipAtTime(time);
    if (!clip) return;
    const cutTime = Math.round(time * FRAME_RATE) / FRAME_RATE;
    if (cutTime <= clip.start || cutTime >= clip.start + clip.duration) return;

    setClips((prev) => {
      const newClips = prev.filter((c) => c.id !== clip.id);
      const first = { ...clip, duration: cutTime - clip.start };
      const second = {
        ...clip,
        id: clip.id + "_b",
        start: cutTime,
        duration: clip.duration - first.duration,
      };
      return [...newClips, first, second].sort((a, b) => a.start - b.start);
    });
  };

  // Save project as JSON file
  const onSaveProject = () => {
    const dataStr = JSON.stringify({ clips }, null, 2);
    const blob = new Blob([dataStr], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `project_${Date.now()}.editor.json`;
    a.click();
    URL.revokeObjectURL(url);
    alert("Project saved.");
  };

  // Load project JSON and update
  const onLoadProject = (e) => {
    const file = e.target.files[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (ev) => {
      try {
        const json = JSON.parse(ev.target.result);
        if (!json.clips) throw new Error("Invalid project file");
        setClips(json.clips);
        setCurrentTime(0);
        setScrollLeft(0);
        setZoom(1);
        alert("Project loaded.");
      } catch {
        alert("Failed to load project file.");
      }
    };
    reader.readAsText(file);
  };

  // Navigation buttons for player
  const goToStart = () => setCurrentTime(0);
  const goToEnd = () => setCurrentTime(getTotalDuration());

  // Go to start/end of current clip under playhead
  const goToClipStart = () => {
    const clip = getClipAtTime(currentTime);
    if (clip) setCurrentTime(clip.start);
  };
  const goToClipEnd = () => {
    const clip = getClipAtTime(currentTime);
    if (clip) setCurrentTime(clip.start + clip.duration);
  };

  // Mouse handlers for cutting tool (show snap line)
  const [hoverPos, setHoverPos] = useState(null);

  const onTimelineMouseMove = (e) => {
    if (timelineRef.current) {
      const rect = timelineRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      if (tool === "cut") {
        const time = posToTime(x);
        const clip = getClipAtTime(time);
        if (clip) {
          // Snap to nearest frame
          let snappedTime = Math.round(time * FRAME_RATE) / FRAME_RATE;
          if (snappedTime < clip.start) snappedTime = clip.start;
          if (snappedTime > clip.start + clip.duration)
            snappedTime = clip.start + clip.duration;
          const pos = timeToPos(snappedTime);
          setHoverPos(pos);
        } else {
          setHoverPos(null);
        }
      }
    }
  };

  const onTimelineClick = (e) => {
    if (tool === "cut" && hoverPos !== null) {
      onCutAt(hoverPos);
    }
  };

  return (
    <div
      onMouseMove={onMouseMove}
      onMouseUp={onMouseUp}
      style={{
        fontFamily: "Arial, sans-serif",
        userSelect: draggingPlayhead ? "none" : "auto",
      }}
    >
      {/* Top menu */}
      <div
        style={{
          padding: 10,
          display: "flex",
          gap: 10,
          borderBottom: "1px solid #ccc",
        }}
      >
        <button onClick={onSaveProject}>Save Project</button>
        <label style={{ cursor: "pointer" }}>
          Load Project
          <input
            type="file"
            accept=".json"
            style={{ display: "none" }}
            onChange={onLoadProject}
          />
        </label>
        <button onClick={() => alert("Export feature coming soon!")}>
          Export Video
        </button>
      </div>

      {/* Player window */}
      <div style={{ padding: 10, borderBottom: "1px solid #ccc" }}>
        {/* Placeholder for video frame */}
        <div
          style={{
            width: "100%",
            height: 180,
            backgroundColor: "#222",
            color: "#ddd",
            fontSize: 36,
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            marginBottom: 10,
            userSelect: "none",
          }}
        >
          Frame at {formatTime(currentTime)}
        </div>

        {/* Playback controls */}
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <button onClick={goToStart} title="Go to Start">
            ⏮
          </button>
          <button onClick={goToClipStart} title="Go to Start of Clip">
            ⏪
          </button>
          <button
            onClick={() => setPlaying(!playing)}
            title={playing ? "Pause" : "Play"}
          >
            {playing ? "⏸" : "▶️"}
          </button>
          <button onClick={goToClipEnd} title="Go to End of Clip">
            ⏩
          </button>
          <button onClick={goToEnd} title="Go to End">
            ⏭
          </button>
          <span>
            {formatTime(currentTime)} / {formatTime(getTotalDuration())}
          </span>
        </div>
      </div>

      <div style={{ display: "flex", height: 120 }}>
        {/* Left tool panel */}
        <div
          style={{
            width: 60,
            borderRight: "1px solid #ccc",
            display: "flex",
            flexDirection: "column",
          }}
        >
          <button
            style={{
              flexGrow: 1,
              backgroundColor: tool === "select" ? "#ddd" : undefined,
              fontWeight: tool === "select" ? "bold" : undefined,
              cursor: "pointer",
              fontSize: 18,
            }}
            onClick={() => setTool("select")}
            title="Select Tool"
          >
            →
          </button>
          <button
            style={{
              flexGrow: 1,
              backgroundColor: tool === "cut" ? "#ddd" : undefined,
              fontWeight: tool === "cut" ? "bold" : undefined,
              cursor: "pointer",
              fontSize: 18,
            }}
            onClick={() => setTool("cut")}
            title="Cut Tool"
          >
            ✂
          </button>
        </div>

        {/* Timeline */}
        <div
          ref={timelineRef}
          onWheel={onWheel}
          onMouseMove={onTimelineMouseMove}
          onClick={onTimelineClick}
          style={{
            position: "relative",
            overflowX: "hidden",
            flexGrow: 1,
            cursor: draggingPlayhead ? "col-resize" : "default",
            userSelect: "none",
            borderBottom: "1px solid #ccc",
            backgroundColor: "#f0f0f0",
          }}
        >
          {/* Time ruler */}
          <div
            style={{
              position: "relative",
              height: 30,
              borderBottom: "1px solid #aaa",
              whiteSpace: "nowrap",
              overflow: "visible",
              paddingLeft: 5,
              paddingRight: 5,
              fontSize: 12,
              fontFamily: "monospace",
              color: "#333",
              userSelect: "none",
            }}
          >
            {[...Array(Math.ceil(getTotalDuration()) + 1)].map((_, i) => {
              const left = i * PIXELS_PER_SECOND * zoom - scrollLeft;
              return (
                <div
                  key={i}
                  style={{
                    position: "absolute",
                    left,
                    top: 0,
                    width: 1,
                    height: 15,
                    backgroundColor: "#666",
                  }}
                >
                  <div
                    style={{
                      position: "absolute",
                      top: 15,
                      left: -10,
                      width: 20,
                      textAlign: "center",
                      userSelect: "none",
                    }}
                  >
                    {i}s
                  </div>
                </div>
              );
            })}

            {/* Vertical red playhead line (draggable) */}
            <div
              onMouseDown={onPlayheadMouseDown}
              style={{
                position: "absolute",
                left: timeToPos(currentTime),
                top: 0,
                width: 2,
                height: 30,
                backgroundColor: "red",
                cursor: "col-resize",
                zIndex: 10,
              }}
            />

            {/* Cut tool hover snap line */}
            {tool === "cut" && hoverPos !== null && (
              <div
                style={{
                  position: "absolute",
                  left: hoverPos,
                  top: 0,
                  width: 2,
                  height: 30,
                  backgroundColor: "blue",
                  pointerEvents: "none",
                }}
              />
            )}
          </div>

          {/* Clips area */}
          <div
            style={{
              position: "relative",
              height: 90,
              whiteSpace: "nowrap",
              padding: "10px 5px",
              boxSizing: "border-box",
              overflowX: "auto",
              scrollBehavior: "smooth",
            }}
          >
            {clips.map((clip) => {
              const left = clip.start * PIXELS_PER_SECOND * zoom - scrollLeft;
              const width = clip.duration * PIXELS_PER_SECOND * zoom;
              return (
                <div
                  key={clip.id}
                  draggable={tool === "select"}
                  onDragStart={(e) => {
                    e.dataTransfer.setDragImage(new Image(), 0, 0);
                    e.dataTransfer.effectAllowed = "move";
                    e.dataTransfer.setData("text/plain", clip.id);
                  }}
                  onDrag={(e) => {
                    if (e.clientX === 0) return; // ignore invalid events
                    const dragLeft =
                      e.clientX -
                      (timelineRef.current?.getBoundingClientRect().left || 0);
                    onClipDrag(
                      clip.id,
                      dragLeft - timeToPos(clip.start) /* deltaX in pixels */,
                    );
                  }}
                  onDragEnd={onClipDragEnd}
                  style={{
                    position: "absolute",
                    left,
                    top: 20,
                    width,
                    height: 50,
                    backgroundColor: "#3a7bd5",
                    color: "white",
                    borderRadius: 4,
                    userSelect: "none",
                    lineHeight: "50px",
                    textAlign: "center",
                    cursor: tool === "select" ? "grab" : "default",
                    boxShadow: "0 0 5px rgba(0,0,0,0.3)",
                  }}
                  title={`${clip.name} [${formatTime(clip.start)} - ${formatTime(clip.start + clip.duration)}]`}
                >
                  {clip.name}
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;
