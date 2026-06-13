#!/usr/bin/env python3
"""Manual validation of schedule slice logic (mirrors C# implementation)."""

from dataclasses import dataclass
from typing import List, Optional, Tuple


@dataclass
class Entry:
    hour: int
    minute: int
    name: str
    duration: int

    @property
    def start(self) -> int:
        return self.hour * 60 + self.minute

    @property
    def is_point(self) -> bool:
        return self.duration == 0

    @property
    def is_activity(self) -> bool:
        return self.duration > 0


ENTRIES = [
    Entry(6, 0, "ActiveCycleRadio", 0),
    Entry(7, 0, "WakeUp", 0),
    Entry(8, 0, "Breakfast", 30),
    Entry(8, 30, "CoffeeAfterBreakfast", 0),
    Entry(9, 0, "Medication", 0),
    Entry(9, 15, "GroupTherapy", 95),
    Entry(11, 0, "Walk", 60),
    Entry(12, 0, "FreeTime", 120),
    Entry(14, 0, "Lunch", 30),
    Entry(14, 30, "CoffeeAfterLunch", 0),
    Entry(15, 0, "Medication", 0),
    Entry(15, 30, "TvPilotAccess", 0),
    Entry(16, 0, "Walk", 30),
    Entry(16, 30, "FreeTime", 150),
    Entry(19, 0, "Dinner", 30),
    Entry(19, 30, "FreeTimeAfterDinner", 150),
    Entry(22, 0, "QuietHours", 480),
]


def within_activity(minute_of_day: int, entry: Entry) -> bool:
    if not entry.is_activity:
        return False
    start = entry.start
    end = start + entry.duration
    if end <= 1440:
        return start <= minute_of_day < end
    wrapped_end = end % 1440
    return minute_of_day >= start or minute_of_day < wrapped_end


def current_activity(minute_of_day: int) -> Optional[str]:
    matches = [e for e in ENTRIES if within_activity(minute_of_day, e)]
    if not matches:
        return None
    return max(matches, key=lambda e: e.start).name


def next_entry(minute_of_day: int) -> str:
    best = None
    best_delta = 10**9
    for e in ENTRIES:
        delta = e.start - minute_of_day
        if delta <= 0:
            delta += 1440
        if delta < best_delta:
            best_delta = delta
            best = e
    return best.name


def point_at(hour: int, minute: int) -> Optional[str]:
    for e in ENTRIES:
        if e.is_point and e.hour == hour and e.minute == minute:
            return e.name
    return None


def activity_start_at(hour: int, minute: int) -> Optional[str]:
    for e in ENTRIES:
        if e.is_activity and e.hour == hour and e.minute == minute:
            return e.name
    return None


def run_case(label: str, hour: int, minute: int, expected_activity: Optional[str], expected_next: str):
    mod = hour * 60 + minute
    act = current_activity(mod)
    nxt = next_entry(mod)
    assert act == expected_activity, f"{label}: activity expected {expected_activity}, got {act}"
    assert nxt == expected_next, f"{label}: next expected {expected_next}, got {nxt}"


def main():
    run_case("start 06:00", 6, 0, None, "WakeUp")
    assert point_at(6, 0) == "ActiveCycleRadio"

    run_case("08:45 gap", 8, 45, None, "Medication")
    run_case("10:00 group", 10, 0, "GroupTherapy", "Walk")
    run_case("15:45 after tv", 15, 45, None, "Walk")
    run_case("20:00 evening free", 20, 0, "FreeTimeAfterDinner", "QuietHours")
    run_case("23:00 quiet", 23, 0, "QuietHours", "ActiveCycleRadio")
    run_case("05:59 quiet", 5, 59, "QuietHours", "ActiveCycleRadio")
    run_case("06:00 morning", 6, 0, None, "WakeUp")

    # jump landing 09:15
    run_case("jump 09:15", 9, 15, "GroupTherapy", "Walk")
    assert activity_start_at(9, 15) == "GroupTherapy"

    # 08:15 still inside breakfast window before a +1h jump
    run_case("08:15 breakfast", 8, 15, "Breakfast", "CoffeeAfterBreakfast")

    print("All schedule validation checks passed.")


if __name__ == "__main__":
    main()
