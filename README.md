# Jellyfin Auto-Organiser Plugin

<div align="center">
    <p>
        <img alt="Logo" src="https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-autoorganiser/master/images/logo.png" height="350"/>
    <br>
        <a href="https://github.com/geo-martino/jellyfin-plugin-autoorganiser/releases"><img alt="Total GitHub Downloads" src="https://img.shields.io/github/downloads/geo-martino/jellyfin-plugin-autoorganiser/total"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-autoorganiser/issues"><img alt="GitHub Issues or Pull Requests" src="https://img.shields.io/github/issues/geo-martino/jellyfin-plugin-autoorganiser"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-autoorganiser/releases"><img alt="Build and Release" src="https://github.com/geo-martino/jellyfin-plugin-autoorganiser/actions/workflows/deploy.yml/badge.svg"/></img></a>
        <a href="https://jellyfin.org/"><img alt="Jellyfin Version" src="https://img.shields.io/badge/Jellyfin-10.11-blue.svg"/></img></a>
    <br>
        <a href="https://github.com/geo-martino/jellyfin-plugin-autoorganiser"><img alt="Code Size" src="https://img.shields.io/github/languages/code-size/geo-martino/jellyfin-plugin-autoorganiser?label=Code%20Size"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-autoorganiser/graphs/contributors"><img alt="Contributors" src="https://img.shields.io/github/contributors/geo-martino/jellyfin-plugin-autoorganiser?logo=github&label=Contributors"/></img></a>
        <a href="https://github.com/geo-martino/jellyfin-plugin-autoorganiser/blob/master/LICENSE"><img alt="License" src="https://img.shields.io/github/license/geo-martino/jellyfin-plugin-autoorganiser?label=License"/></img></a>
    </p>
</div>

This plugin automatically organises and renames files as recommended by Jellyfin for
- [Movies](https://jellyfin.org/docs/general/server/media/movies)
- [Shows](https://jellyfin.org/docs/general/server/media/shows)

## ✨ Features

- **Automatic Renaming**: Renames files to follow standard conventions (e.g., Series (Year) - S01E01 - Episode Title.ext).
- **Library Sorting**: Moves or copies files into your structured library folders (e.g., /Media/TV Shows/Show Name/Season 01/).
- **Duplicate Handling**: Options to overwrite existing files or skip duplicates.
- **Clean Up**: Automatically removes empty source folders after successful organization.
- **Scheduled Organisation**: Integration with Jellyfin's Scheduled Tasks to periodically re-evaluate and organise files for newly added or changed items.

## Configuration

You may configure the plugin via the Jellyfin UI by going to the plugin's settings page. You will be able to configure from the options as shown below.

<div align="center">
    <p>
        <img alt="Configuration page 1" src="https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-autoorganiser/master/images/config_1.png" width="600"/>
        <img alt="Configuration page 2" src="https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-autoorganiser/master/images/config_2.png" width="600"/>
    </p>
</div>

## 📦 How to Install

1. Add this repository URL to your Jellyfin plugin catalog:
```
https://raw.githubusercontent.com/geo-martino/jellyfin-plugin-repository/master/manifest.json
```
2. Install the plugin
3. Restart Jellyfin

## 🤝 Contributing
Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.
