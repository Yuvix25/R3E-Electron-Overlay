import SettingComponent from "./SettingComponent.js";


export default class ToggleSetting extends SettingComponent {
    public static override readonly elementName = 'toggle-setting';

    private checkbox: HTMLInputElement;

    protected override _enable(): void {
        this.checkbox.disabled = false;
    }
    protected override _disable(): void {
        this.checkbox.disabled = true;
    }

    override connected() {
        requestAnimationFrame(() => {
            this.innerHTML = '';

            const label = document.createElement('label');
            label.innerText = this.text;

            const switchContainer = document.createElement('div');
            switchContainer.classList.add('switch');

            this.checkbox = document.createElement('input');
            this.checkbox.type = 'checkbox';
            this.checkbox.classList.add('switch__input');
            this.checkbox.id = 'switch-' + this.key;

            const switchLabel = document.createElement('label');
            switchLabel.classList.add('switch__label');
            switchLabel.setAttribute('for', this.checkbox.id);

            switchContainer.appendChild(this.checkbox);
            switchContainer.appendChild(switchLabel);

            this.checkbox.addEventListener('input', () => {
                this.onValueChange(this.checkbox.checked);
            });

            this.appendChild(label);
            this.appendChild(switchContainer);

            this.initializationDone();
        });
    }

    protected valueChange(val: boolean) {
        this.value = val;
        this.checkbox.checked = val;
    }
}
