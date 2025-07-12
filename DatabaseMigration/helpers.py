from datetime import datetime, timezone
import string


def datetime_to_ticks(dt: datetime) -> int:
    epoch_start = datetime(1, 1, 1, tzinfo=timezone.utc)
    ticks_per_second = 10_000_000
    delta = dt - epoch_start
    return int(delta.total_seconds() * ticks_per_second)


def merge_without_overwrite[K, V](
    base_dict: dict[K, V], new_dict: dict[K, V]
) -> dict[K, V]:
    for key, value in new_dict.items():
        if key not in base_dict:
            base_dict[key] = value
    return base_dict


def is_symbol_or_space_only(s: str):
    return all(c in string.punctuation or c.isspace() for c in s)
