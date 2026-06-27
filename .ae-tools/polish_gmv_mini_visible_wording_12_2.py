import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "data/equipment-diagnostics/error-knowledge/gree/gmv-mini"
DATE = "2026-06-27T00:00:00Z"

MEANING_RU = {
    "Abnormal address for indoor unit": "некорректный адрес внутреннего блока",
    "Abnormal clock of system": "ненормальная работа системных часов",
    "Abnormal code-dialing setting of capacity": "некорректная настройка мощности DIP-переключателем",
    "Abnormal PCB for wired controller": "неисправность платы проводного пульта",
    "AC current protection for inverter compressor": "защита инверторного компрессора по AC-току",
    "Address shock of system": "конфликт адресов системы",
    "Air-mixing protection for 4-way valve": "защита 4-ходового клапана по смешению потоков",
    "Alarm due to abnormal valve": "предупреждение о ненормальной работе клапана",
    "Alarming due to bad air quality": "предупреждение о плохом качестве воздуха",
    "Alarming due to engineering series number shock of indoor unit": "предупреждение о конфликте инженерного номера серии внутреннего блока",
    "Alarming due to wrong quantity of outdoor unit": "предупреждение о неверном количестве наружных блоков",
    "Allocate addresses": "распределение адресов",
    "Capacity code of outdoor unit/wrong setting of jumper cap": "неверный код мощности наружного блока или настройка перемычки",
    "Charge refrigerant automatically": "автоматическая заправка хладагентом",
    "Charge refrigerant by hand": "ручная заправка хладагентом",
    "Circuit malfunction of driven current detection for compressor": "неисправность цепи контроля тока привода компрессора",
    "Clean alarming for filter": "предупреждение о необходимости очистки фильтра",
    "Communication malfunction": "нарушение связи",
    "Communication malfunction between indoor unit receiving lamp board": "нарушение связи с платой индикации внутреннего блока",
    "Confirm preheated compressor": "подтверждение предварительного прогрева компрессора",
    "Confirm the quantity of indoor unit": "подтверждение количества внутренних блоков",
    "Confirm the quantity of outdoor unit": "подтверждение количества наружных блоков",
    "Cooling only model": "модель только для охлаждения",
    "Debugging for unit": "режим наладки блока",
    "Defrosting": "оттайка",
    "Defrosting period K1 setting": "настройка периода оттайки K1",
    "Desynchronizing protection for inverter compressor": "защита инверторного компрессора по рассинхронизации",
    "Detect indoor unit components": "проверка компонентов внутреннего блока",
    "Detect outdoor unit components": "проверка компонентов наружного блока",
    "Detect outdoor unit internal communication": "проверка внутренней связи наружного блока",
    "Discharge high temperature protection for compressor": "защита компрессора по высокой температуре нагнетания",
    "Distribution overflow of IP address": "переполнение распределения сетевых адресов",
    "Driven board of compressor works abnormally": "ненормальная работа платы привода компрессора",
    "Driven communication malfunction between main board and inverter compressor": "нарушение связи между главной платой и инверторным компрессором",
    "Driven IPM module protection for compressor": "защита IPM-модуля привода компрессора",
    "Driven PFC protection of compressor": "защита PFC привода компрессора",
    "E-heater protection": "защита электронагревателя",
    "Emergency status of compressor": "аварийный статус компрессора",
    "Emergency status of fan": "аварийный статус вентилятора",
    "Emergency status of module": "аварийный статус модуля",
    "Emergency stop": "аварийная остановка",
    "Engineering series number inquiry for indoor unit": "просмотр инженерного номера серии внутреннего блока",
    "EU AA class energy efficiency test mode": "тестовый режим энергоэффективности класса EU AA",
    "Failure start up for inverter compressor": "сбой запуска инверторного компрессора",
    "Fan blow": "продувка вентилятором",
    "Fan model": "вентиляторная модель",
    "Freeze prevention protection": "защита от замерзания",
    "Heat pump function setting": "настройка функции теплового насоса",
    "Heat pump unit": "блок теплового насоса",
    "Heating": "нагрев",
    "Heating only model": "модель только для нагрева",
    "High pressure is too low": "слишком низкое значение высокого давления",
    "High pressure protection": "защита по высокому давлению",
    "High pressure ratio protection of system": "защита системы по высокому отношению давлений",
    "High rated capacity": "высокая номинальная производительность",
    "High voltage protection for driven DC bus bar of compressor": "защита DC-шины привода компрессора по высокому напряжению",
    "Indoor fan protection": "защита вентилятора внутреннего блока",
    "Insufficient power supply": "недостаточное питание",
    "Insufficient refrigerant protection": "защита при недостатке хладагента",
    "IPLV test": "IPLV-тест",
    "Limit operation": "ограниченный режим работы",
    "Limit setting for the maximum output capacity": "настройка ограничения максимальной выходной мощности",
    "Long-distance emergency stop": "удаленная аварийная остановка",
    "Loose protection for discharge temperature sensor for compressor 1": "защита при плохом контакте датчика температуры нагнетания компрессора 1",
    "Low pressure protection": "защита по низкому давлению",
    "Low pressure ratio protection of system": "защита системы по низкому отношению давлений",
    "Low rated capacity": "низкая номинальная производительность",
    "Low temperature protection for drive module": "защита модуля привода по низкой температуре",
    "Low voltage protection for driven DC bus bar of compressor": "защита DC-шины привода компрессора по низкому напряжению",
    "Low-temperature protection for discharge": "защита по низкой температуре нагнетания",
    "Malfunction driven board for compressor": "неисправность платы привода компрессора",
    "Malfunction for outdoor ambient temperature sensor": "неисправность датчика температуры наружного воздуха",
    "Malfunction for temperature sensor of exit tube of gas and liquid separator (exit tube A)": "неисправность датчика температуры выходной трубки газожидкостного сепаратора, трубка A",
    "Malfunction for temperature sensor of inlet tube of gas and liquid separator": "неисправность датчика температуры входной трубки газожидкостного сепаратора",
    "Malfunction inquiry": "просмотр неисправностей",
    "Malfunction of air exhause temperature sensor": "неисправность датчика температуры отводимого воздуха",
    "Malfunction of ambient temperature sensor": "неисправность датчика температуры воздуха",
    "Malfunction of DC motor": "неисправность DC-двигателя",
    "Malfunction of defrosting temperature sensor 1": "неисправность датчика температуры оттайки 1",
    "Malfunction of defrosting temperature sensor 2": "неисправность датчика температуры оттайки 2",
    "Malfunction of discharge temperature sensor for compressor 1": "неисправность датчика температуры нагнетания компрессора 1",
    "Malfunction of driven charging loop for compressor": "неисправность цепи зарядки привода компрессора",
    "Malfunction of driven temperature sensor for compressor": "неисправность датчика температуры привода компрессора",
    "Malfunction of entry tube temperature sensor": "неисправность датчика температуры входной трубки",
    "Malfunction of exit tube temperature sensor": "неисправность датчика температуры выходной трубки",
    "Malfunction of gas exit temperature sensor for heat exchanger": "неисправность датчика температуры газа на выходе теплообменника",
    "Malfunction of gas temperature sensor for subcooler": "неисправность датчика температуры газа переохладителя",
    "Malfunction of high pressure sensor": "неисправность датчика высокого давления",
    "Malfunction of humidity sensor": "неисправность датчика влажности",
    "Malfunction of indoor CO2 sensor": "неисправность датчика CO2 внутреннего блока",
    "Malfunction of indoor unit": "неисправность внутреннего блока",
    "Malfunction of indoor unit-lacking": "неисправность из-за отсутствующего внутреннего блока",
    "Malfunction of jumper cap": "неисправность перемычки",
    "Malfunction of liquid temperature sensor for subcooler": "неисправность датчика температуры жидкости переохладителя",
    "Malfunction of low pressure sensor": "неисправность датчика низкого давления",
    "Malfunction of main control unit": "неисправность главного блока управления",
    "Malfunction of outdoor unit": "неисправность наружного блока",
    "Malfunction of pipeline for indoor unit": "неисправность трубопровода внутреннего блока",
    "Malfunction of pipeline for outdoor unit": "неисправность трубопровода наружного блока",
    "Malfunction of water temperature sensor": "неисправность датчика температуры воды",
    "Mode shock": "конфликт режима работы",
    "Negative code": "отрицательный код",
    "No main indoor unit": "не назначен главный внутренний блок",
    "No malfunction of main control unit": "отсутствие неисправности главного блока управления",
    "Oil return": "возврат масла",
    "On-line test": "онлайн-тест",
    "Operational parameter inquiry of compressor": "просмотр рабочих параметров компрессора",
    "Other module protection": "защита другого модуля",
    "Overcurrent protection for compressor 1": "защита компрессора 1 по перегрузке тока",
    "Overcurrent protection for inverter compressor": "защита инверторного компрессора по перегрузке тока",
    "Overheating protection for driven IPM of compressor": "защита IPM привода компрессора по перегреву",
    "Parameters inquiry": "просмотр параметров",
    "Phase-losing of inverter compressor": "обрыв фазы инверторного компрессора",
    "Poor indoor PCB": "неисправность платы управления внутреннего блока",
    "Poor main board of outdoor unit": "неисправность главной платы наружного блока",
    "Power supply of wired controller is faulted": "неисправность питания проводного пульта",
    "Power voltage protection for the driven board of compressor": "защита платы привода компрессора по напряжению питания",
    "Preheat time is not enough for compressor": "недостаточное время предварительного прогрева компрессора",
    "Quit mode setting": "настройка режима выхода",
    "Refrigerant recovery": "режим сбора хладагента",
    "Refrigerant-charging is invalid": "некорректный режим заправки хладагентом",
    "Reset protection for the driven module of compressor": "защита модуля привода компрессора по сбросу",
    "SE setting for the operation": "настройка SE для работы системы",
    "Set master unit": "назначение главного блока",
    "Setting for indoor unit and outdoor unit is succeeded": "настройка внутреннего и наружного блоков успешно выполнена",
    "Special code: engineering debugging code": "инженерный код наладки",
    "Startup debugging confirmation of unit": "подтверждение пусконаладки блока",
    "The indoor unit model can't match with outdoor unit model": "модель внутреннего блока не соответствует наружному блоку",
    "Upper limit setting for the collocation matching ratio for indoor unit and outdoor unit": "настройка предела коэффициента соответствия внутренних и наружных блоков",
    "Vacuum pump mode": "режим вакуумирования",
    "Water overflow protection": "защита от переполнения водой",
    "Wrong address for the driven board of compressor": "неверный адрес платы привода компрессора",
    "Wrong code-dialing during emergency operation": "неверная настройка DIP-переключателя при аварийном режиме",
    "Wrong number of indoor unit for one-to-more indoor unit": "неверное количество внутренних блоков в мульти-системе",
    "Wrong series for one-to-more indoor unit": "неверная серия внутреннего блока в мульти-системе",
}

STATUS_SIGNALS = {"Status", "Debug", "Commissioning", "Maintenance"}


def read_json(path: Path) -> dict:
    with path.open("r", encoding="utf-8-sig") as handle:
        return json.load(handle)


def write_json(path: Path, data: dict) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(data, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def text_payload(entry: dict, meaning_ru: str, audience: str) -> dict:
    code = entry["code"]
    signal = entry["signalType"]
    status_like = signal in STATUS_SIGNALS
    title = f"Gree GMV Mini {code} — {meaning_ru}"

    if status_like:
        summary = (
            f"Код {code} для GMV Mini относится к состоянию или сервисной функции: {meaning_ru}. "
            "Это не самостоятельный признак отказа компонента."
        )
        safety = "Не меняйте сервисные настройки без инструкции производителя и нужной квалификации."
        checks = [
            f"Зафиксируйте код {code}, режим работы и место отображения.",
            "Проверьте, не идет ли штатная наладка, настройка или просмотр параметров.",
            "Если вместе с кодом есть отказ работы, передайте сервису сопутствующие коды и симптомы.",
        ]
        do_not = [
            f"Не считайте {code} аварией без других признаков.",
            "Не меняйте параметры платы только по этому коду.",
        ]
        action = (
            "Если это штатный режим просмотра или настройки, отдельное действие не требуется; "
            "при жалобе на работу нужна сервисная диагностика."
        )
    else:
        summary = (
            f"Код {code} для GMV Mini относится к событию: {meaning_ru}. "
            "Точная причина зависит от модели, условий появления и сопутствующих кодов."
        )
        safety = (
            "Проверки внутри электрических отсеков и холодильного контура выполняет "
            "квалифицированный специалист."
        )
        checks = [
            f"Зафиксируйте код {code}, модель оборудования и место отображения.",
            "Проверьте, нет ли рядом других кодов и не менялись ли условия работы системы.",
            "Передайте сервисному специалисту код, модель и обстоятельства появления.",
        ]
        do_not = [
            f"Не делайте вывод о замене компонента только по коду {code}.",
            "Не выполняйте повторные сбросы, если код возвращается.",
        ]
        action = (
            "Обратитесь в квалифицированный сервис и сопоставьте код с фактическими симптомами "
            "и таблицей руководства."
        )

    return {
        "locale": "ru",
        "possibleCauses": [],
        "isMachineTranslated": False,
        "isReviewed": True,
        "createdAt": entry["createdAt"],
        "updatedAt": DATE,
        "id": f"{entry['id']}-ru-{audience.lower()}",
        "audience": audience,
        "title": title,
        "summary": summary,
        "safetyNote": safety,
        "checkSteps": checks,
        "doNotAdvise": do_not,
        "recommendedAction": action,
        "sourceNote": "Данные сверены с сервисным руководством GMV Mini.",
    }


def main() -> None:
    files = sorted(RUNTIME.rglob("*.json"))
    if len(files) != 136:
        raise RuntimeError(f"Expected 136 GMV Mini JSON files, got {len(files)}.")

    missing: list[str] = []
    changed_files = 0
    changed_texts = 0

    for path in files:
        entry = read_json(path)
        source_meaning = entry["sourceMeaning"]
        meaning_ru = MEANING_RU.get(source_meaning)
        if meaning_ru is None:
            missing.append(f"{entry['id']}: {source_meaning}")
            continue

        before = json.dumps(entry.get("texts", []), ensure_ascii=False, sort_keys=True)
        entry["texts"] = [text_payload(entry, meaning_ru, audience) for audience in ("Consumer", "Installer", "Engineer")]
        entry["updatedAt"] = DATE
        after = json.dumps(entry["texts"], ensure_ascii=False, sort_keys=True)
        if before != after:
            changed_files += 1
            changed_texts += len(entry["texts"])
            write_json(path, entry)

    if missing:
        raise RuntimeError("Missing GMV Mini sourceMeaning translations:\n" + "\n".join(sorted(missing)))

    print(f"checked_files={len(files)}")
    print(f"unique_source_meanings={len({read_json(path)['sourceMeaning'] for path in files})}")
    print(f"changed_files={changed_files}")
    print(f"changed_visible_texts={changed_texts}")


if __name__ == "__main__":
    main()
