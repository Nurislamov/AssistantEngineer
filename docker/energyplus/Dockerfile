# Используем Ubuntu 22.04 как базовый образ
FROM ubuntu:22.04

# Устанавливаем необходимые зависимости
RUN apt-get update && \
    apt-get install -y \
    wget \
    expect \
    libgomp1 \
    libx11-6 && \
    rm -rf /var/lib/apt/lists/*

# Скачиваем установочный скрипт EnergyPlus 24.1.0
RUN wget https://github.com/NREL/EnergyPlus/releases/download/v24.1.0/EnergyPlus-24.1.0-9d7789a3ac-Linux-Ubuntu22.04-x86_64.sh

# Делаем скрипт исполняемым
RUN chmod +x EnergyPlus-24.1.0-9d7789a3ac-Linux-Ubuntu22.04-x86_64.sh

# Создаём скрипт автоматического ответа на вопросы лицензии
RUN echo 'set timeout 120' > install.exp && \
    echo 'spawn ./EnergyPlus-24.1.0-9d7789a3ac-Linux-Ubuntu22.04-x86_64.sh' >> install.exp && \
    echo 'expect -re {Do you accept the license.*:}' >> install.exp && \
    echo 'send "y\r"' >> install.exp && \
    echo 'expect -re {EnergyPlus install directory.*}' >> install.exp && \
    echo 'send "\r"' >> install.exp && \
    echo 'expect -re {Symbolic link location.*}' >> install.exp && \
    echo 'send "\r"' >> install.exp && \
    echo 'expect eof' >> install.exp

# Запускаем установку с помощью expect
RUN expect install.exp

# Устанавливаем рабочую директорию
WORKDIR /workspace

# Команда по умолчанию
ENTRYPOINT ["energyplus"]