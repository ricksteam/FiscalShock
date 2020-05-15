# Continuous Integration / Continuous Deployment
The `.travis.yml`, several scripts in this directory, and the file `fiscal-shock/Assets/Editor/BuildScript.cs` were adapted from [Gabriel Le Breton's examples](https://gitlab.com/gableroux/unity3d-gitlab-ci-example/-/tree/master) (MIT licensed). Changes include setting the correct project path, changing the Unity version, and adding calls to scripts on failure. Most of the Travis configuration was rewritten from the original to suit our needs.

A valid `Unity_v2019.x.ulf.enc` file is **required** to run automated tests or create builds in any generic CI/CD service, like Travis CI, Circle CI, or GitLab.

# Updating a fork to use your own Travis CI access and machine user
## Machine user
1. Create a new GitHub account to serve as a machine user. It is not recommended to use your regular GitHub account to execute deployments for security reasons.
1. Generate a new [GitHub personal access token](https://github.com/settings/tokens) on the machine user's account. Copy the token.
1. Add the following environment variables to your Travis CI settings for the repo:
    - `GITHUB_TOKEN`: This is the personal access token for your machine user. It's required to push changes to documentation or create releases. It should not be needed if you do not plan on setting up continuous deployment.
    - `WEBHOOK_URL`: This is a Discord channel webhook URL. You should disable the Discord script in `.travis.yml` if you don't want to set up notifications in a Discord channel.

## Updating Travis
1. Review the instructions in the link above. These were the main guide to setting up CI/CD.
1. Disable any unwanted tasks in `.travis.yml`.
1. Generate a new `Unity_v2019.x.ulf` file by following the steps [here](https://gitlab.com/gableroux/unity3d-gitlab-ci-example/-/tree/master#b-locally), using the 2019.3 Docker image and your own Unity credentials.
1. Encrypt your newly-created `Unity_v2019.x.ulf` into `Unity_v2019.x.ulf.enc` with Travis using the steps [here](https://docs.travis-ci.com/user/encrypting-files/). The `.ulf.enc` file must exist in the root directory of the repo.
1. Log in to your Travis CI and check the settings for your copy of the repository. You should see two new environment variables: `encrypted_[HEX_STRING]_iv` and `encrypted_[HEX_STRING]_key`. The hex string is different each time you encrypt a file. You need to update `ci/travis_prepare_workspace.sh` to use the new hex string in your copy of the repository.
1. You also need to update the Travis scripts to download and import the asset unitypackage, rather than pulling the LFS files.
    - [This](https://docs.unity3d.com/Manual/CommandLineArguments.html) and some other internet research should put you on the right path.
1. Push your updates to the `.ulf.enc`, Travis configuration, and scripts.
1. Wait for a build.

## Travis Auto-Tagging
If you choose to use auto-builds/releases via the provided scripts, you might want to change the versioning. Assuming that you don't update these scripts, the following guide applies.

The major and minor version numbers (the first two) must be changed by pushing a new [annotated git tag](https://git-scm.com/book/en/v2/Git-Basics-Tagging) to your repo. This tag should follow the syntax `vX.Y.0`, where `X` is the major version, `Y` is the minor version, and `0` is the patch version -- which should be zero any time you increment the major/minor versions.

Example:
1. Wait for any running Travis builds to finish (when they tag the release, they'll screw up the script that checks for the latest version).
1. Create a new annotated tag locally. The annotation doesn't need to be anything meaningful.
1. Push the tag to the remote repository.
